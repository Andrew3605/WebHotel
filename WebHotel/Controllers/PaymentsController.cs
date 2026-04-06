using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.Models;
using WebHotel.ViewModels;

namespace WebHotel.Controllers
{
    [Authorize(Roles = "Customer")]
    public class PaymentsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public PaymentsController(AppDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        // GET: /Payments/Pay/5
        public async Task<IActionResult> Pay(int bookingId)
        {
            var user = await _users.GetUserAsync(User);
            if (user?.CustomerId == null) return Forbid();

            var b = await _db.Bookings
                .Include(x => x.Room)
                .FirstOrDefaultAsync(x => x.Id == bookingId && x.CustomerId == user.CustomerId);

            if (b == null) return NotFound();
            if (b.IsPaid) return RedirectToAction("My", "Bookings");

            var vm = new PaymentVm
            {
                BookingId = b.Id,
                RoomNumber = b.Room?.Number ?? "(room)",
                CheckIn = b.CheckIn,
                CheckOut = b.CheckOut,
                TotalPrice = b.TotalPrice,

                // sensible defaults
                ExpMonth = Math.Clamp(DateTime.UtcNow.Month, 1, 12),
                ExpYear = DateTime.UtcNow.Year
            };

            return View(vm);
        }

        // POST: /Payments/Pay
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(PaymentVm vm)
        {
            var user = await _users.GetUserAsync(User);
            if (user?.CustomerId == null) return Forbid();

            var b = await _db.Bookings
                .Include(x => x.Room)
                .FirstOrDefaultAsync(x => x.Id == vm.BookingId && x.CustomerId == user.CustomerId);

            if (b == null) return NotFound();
            if (b.IsPaid) return RedirectToAction("My", "Bookings");

            // extra validations (simple, no real gateway)
            if (vm.ExpYear < DateTime.UtcNow.Year ||
               (vm.ExpYear == DateTime.UtcNow.Year && vm.ExpMonth < DateTime.UtcNow.Month))
            {
                ModelState.AddModelError(nameof(vm.ExpMonth), "Card is expired.");
            }

            if (!ModelState.IsValid)
            {
                // re-fill summary for the view
                vm.RoomNumber = b.Room?.Number ?? "(room)";
                vm.CheckIn = b.CheckIn;
                vm.CheckOut = b.CheckOut;
                vm.TotalPrice = b.TotalPrice;
                return View(vm);
            }

            // “Process payment” (pretend success)
            b.IsPaid = true;
            await _db.SaveChangesAsync();

            TempData["PaymentOk"] = $"Payment received for booking #{b.Id}.";
            return RedirectToAction("My", "Bookings");
        }
    }
}
