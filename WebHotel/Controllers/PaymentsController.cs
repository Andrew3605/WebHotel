using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.Models;
using WebHotel.Services;
using WebHotel.ViewModels;

namespace WebHotel.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IHotelEmailSender _email;
        private readonly IAuditService _audit;

        public PaymentsController(AppDbContext db, UserManager<ApplicationUser> users,
            IHotelEmailSender email, IAuditService audit)
        {
            _db = db;
            _users = users;
            _email = email;
            _audit = audit;
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Pay(int bookingId)
        {
            var user = await _users.GetUserAsync(User);
            if (user?.CustomerId == null) return Forbid();

            var booking = await LoadCustomerBookingAsync(bookingId, user.CustomerId.Value);
            if (booking == null) return NotFound();
            if (booking.BalanceDue <= 0) return RedirectToAction(nameof(Statement), new { bookingId });

            var vm = BuildPaymentVm(booking);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Pay(PaymentVm vm)
        {
            var user = await _users.GetUserAsync(User);
            if (user?.CustomerId == null) return Forbid();

            var booking = await LoadCustomerBookingAsync(vm.BookingId, user.CustomerId.Value);
            if (booking == null) return NotFound();

            HydratePaymentVm(vm, booking);

            if (vm.ExpYear < DateTime.UtcNow.Year ||
               (vm.ExpYear == DateTime.UtcNow.Year && vm.ExpMonth < DateTime.UtcNow.Month))
            {
                ModelState.AddModelError(nameof(vm.ExpMonth), "Card is expired.");
            }

            var amountToCharge = ResolveCustomerPaymentAmount(vm, booking);
            if (amountToCharge <= 0)
                ModelState.AddModelError(string.Empty, "This booking has no outstanding balance.");

            if (!ModelState.IsValid)
                return View(vm);

            var last4 = vm.CardNumber.Length >= 4
                ? vm.CardNumber[^4..]
                : vm.CardNumber;

            var entry = new PaymentEntry
            {
                BookingId = booking.Id,
                Type = PaymentEntryType.Payment,
                Method = PaymentMethod.Card,
                Description = vm.PaymentOption == BookingPaymentOption.Deposit
                    ? "Online booking deposit"
                    : "Online balance payment",
                Amount = amountToCharge,
                Reference = $"WEB-{booking.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                MaskedCardNumber = $"**** **** **** {last4}",
                ProcessedBy = user.Email ?? user.UserName,
                ProcessedAt = DateTime.UtcNow
            };

            booking.PaymentEntries.Add(entry);
            _db.PaymentEntries.Add(entry);
            UpdateBookingPaymentStatus(booking);

            await _db.SaveChangesAsync();

            // Send payment receipt email
            if (booking.Customer != null)
            {
                var newBalance = BookingPaymentCalculator.GetBalanceDue(booking.TotalPrice, booking.PaymentEntries);
                await _email.SendPaymentReceiptAsync(booking.Customer.Email, booking.Customer.FullName,
                    booking.Id, amountToCharge, Math.Max(0, newBalance));
            }

            TempData["PaymentOk"] = $"Payment of {amountToCharge:C} was recorded for booking #{booking.Id}.";
            return RedirectToAction(nameof(Statement), new { bookingId = booking.Id });
        }

        [Authorize(Roles = "Customer,Admin,Staff")]
        public async Task<IActionResult> Statement(int bookingId)
        {
            var booking = await LoadBookingForCurrentUserAsync(bookingId);
            if (booking == null) return NotFound();

            return View(BuildStatementVm(booking, User.IsInRole("Admin") || User.IsInRole("Staff")));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeskEntry(FrontDeskEntryVm vm)
        {
            var booking = await LoadAdminBookingAsync(vm.BookingId);
            if (booking == null) return NotFound();

            vm.Description = vm.Description?.Trim() ?? string.Empty;
            vm.Reference = string.IsNullOrWhiteSpace(vm.Reference) ? null : vm.Reference.Trim();

            if (vm.Type == PaymentEntryType.Payment && vm.Amount > booking.BalanceDue)
            {
                ModelState.AddModelError(nameof(vm.Amount), "Payment exceeds the current balance due.");
            }

            var refundableAmount = BookingPaymentCalculator.GetRefundableAmount(booking.PaymentEntries);
            if (vm.Type == PaymentEntryType.Refund && vm.Amount > refundableAmount)
            {
                ModelState.AddModelError(nameof(vm.Amount), "Refund exceeds the amount already collected.");
            }

            if (!ModelState.IsValid)
            {
                var statementVm = BuildStatementVm(booking, canManageFrontDesk: true);
                statementVm.FrontDeskEntry = vm;
                return View("Statement", statementVm);
            }

            var user = await _users.GetUserAsync(User);

            var entry = new PaymentEntry
            {
                BookingId = booking.Id,
                Type = vm.Type,
                Method = vm.Method,
                Description = vm.Description,
                Amount = vm.Amount,
                Reference = vm.Reference,
                ProcessedBy = user?.Email ?? User.Identity?.Name,
                ProcessedAt = DateTime.UtcNow
            };

            booking.PaymentEntries.Add(entry);
            _db.PaymentEntries.Add(entry);
            UpdateBookingPaymentStatus(booking);

            await _db.SaveChangesAsync();

            await _audit.LogAsync($"Payment.{vm.Type}", "PaymentEntry", entry.Id,
                $"Booking #{booking.Id}, {vm.Amount:C} via {vm.Method}", User);

            TempData["PaymentOk"] = vm.Type switch
            {
                PaymentEntryType.Charge => $"Charge of {vm.Amount:C} added to booking #{booking.Id}.",
                PaymentEntryType.Refund => $"Refund of {vm.Amount:C} recorded for booking #{booking.Id}.",
                _ => $"Payment of {vm.Amount:C} recorded for booking #{booking.Id}."
            };

            return RedirectToAction(nameof(Statement), new { bookingId = booking.Id });
        }

        private async Task<Booking?> LoadCustomerBookingAsync(int bookingId, int customerId)
        {
            return await _db.Bookings
                .Include(x => x.Room)
                .Include(x => x.Customer)
                .Include(x => x.PaymentEntries)
                .FirstOrDefaultAsync(x => x.Id == bookingId && x.CustomerId == customerId);
        }

        private async Task<Booking?> LoadAdminBookingAsync(int bookingId)
        {
            return await _db.Bookings
                .Include(x => x.Room)
                .Include(x => x.Customer)
                .Include(x => x.PaymentEntries)
                .FirstOrDefaultAsync(x => x.Id == bookingId);
        }

        private async Task<Booking?> LoadBookingForCurrentUserAsync(int bookingId)
        {
            if (User.IsInRole("Admin"))
                return await LoadAdminBookingAsync(bookingId);

            var user = await _users.GetUserAsync(User);
            if (user?.CustomerId == null)
                return null;

            return await LoadCustomerBookingAsync(bookingId, user.CustomerId.Value);
        }

        private PaymentVm BuildPaymentVm(Booking booking)
        {
            var vm = new PaymentVm
            {
                BookingId = booking.Id,
                RoomNumber = booking.Room?.Number ?? "(room)",
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                RoomTotal = booking.TotalPrice,
                ExtraCharges = booking.ExtraCharges,
                PaidToDate = booking.PaymentsReceived,
                BalanceDue = booking.BalanceDue,
                DepositAmount = BookingPaymentCalculator.GetDepositAmount(booking.TotalPrice, booking.PaymentEntries),
                HasPreviousPayments = booking.PaymentsReceived > 0,
                PaymentOption = booking.PaymentsReceived > 0
                    ? BookingPaymentOption.OutstandingBalance
                    : BookingPaymentOption.Deposit,
                ExpMonth = Math.Clamp(DateTime.UtcNow.Month, 1, 12),
                ExpYear = DateTime.UtcNow.Year
            };

            return vm;
        }

        private void HydratePaymentVm(PaymentVm vm, Booking booking)
        {
            vm.RoomNumber = booking.Room?.Number ?? "(room)";
            vm.CheckIn = booking.CheckIn;
            vm.CheckOut = booking.CheckOut;
            vm.RoomTotal = booking.TotalPrice;
            vm.ExtraCharges = booking.ExtraCharges;
            vm.PaidToDate = booking.PaymentsReceived;
            vm.BalanceDue = booking.BalanceDue;
            vm.DepositAmount = BookingPaymentCalculator.GetDepositAmount(booking.TotalPrice, booking.PaymentEntries);
            vm.HasPreviousPayments = booking.PaymentsReceived > 0;
        }

        private decimal ResolveCustomerPaymentAmount(PaymentVm vm, Booking booking)
        {
            if (booking.BalanceDue <= 0)
                return 0m;

            if (vm.PaymentOption == BookingPaymentOption.Deposit)
            {
                if (booking.PaymentsReceived > 0)
                {
                    ModelState.AddModelError(nameof(vm.PaymentOption), "Deposit is only available for the first payment.");
                    return 0m;
                }

                return BookingPaymentCalculator.GetDepositAmount(booking.TotalPrice, booking.PaymentEntries);
            }

            return booking.BalanceDue;
        }

        private static void UpdateBookingPaymentStatus(Booking booking)
        {
            booking.IsPaid = BookingPaymentCalculator.IsFullyPaid(booking.TotalPrice, booking.PaymentEntries);
        }

        private BookingStatementVm BuildStatementVm(Booking booking, bool canManageFrontDesk)
        {
            return new BookingStatementVm
            {
                BookingId = booking.Id,
                CustomerName = booking.Customer?.FullName ?? $"Customer #{booking.CustomerId}",
                CustomerEmail = booking.Customer?.Email ?? "-",
                RoomNumber = booking.Room?.Number ?? "-",
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                RoomTotal = booking.TotalPrice,
                ExtraCharges = booking.ExtraCharges,
                PaymentsReceived = booking.PaymentsReceived,
                RefundTotal = booking.RefundTotal,
                BalanceDue = booking.BalanceDue,
                IsPaid = booking.IsPaid,
                CanManageFrontDesk = canManageFrontDesk,
                Entries = booking.PaymentEntries
                    .OrderByDescending(x => x.ProcessedAt)
                    .ThenByDescending(x => x.Id)
                    .ToList(),
                FrontDeskEntry = new FrontDeskEntryVm
                {
                    BookingId = booking.Id,
                    Type = PaymentEntryType.Charge,
                    Method = PaymentMethod.PosTerminal
                }
            };
        }
    }
}
