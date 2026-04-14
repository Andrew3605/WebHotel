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
    [Authorize(Roles = "Admin,Staff")]
    public class RoomServiceController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IAuditService _audit;

        public RoomServiceController(AppDbContext db, UserManager<ApplicationUser> users, IAuditService audit)
        {
            _db = db;
            _users = users;
            _audit = audit;
        }

        // GET: /RoomService/Order?bookingId=5
        public async Task<IActionResult> Order(int bookingId, string? category)
        {
            var booking = await _db.Bookings
                .Include(b => b.Room)
                .Include(b => b.Customer)
                .Include(b => b.PaymentEntries)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            var products = await _db.Products.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync();
            var categories = products.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();

            if (!string.IsNullOrWhiteSpace(category))
                products = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

            var vm = new RoomServiceVm
            {
                BookingId = booking.Id,
                RoomNumber = booking.Room?.Number ?? "-",
                CustomerName = booking.Customer?.FullName ?? $"Customer #{booking.CustomerId}",
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                BalanceDue = booking.BalanceDue,
                CategoryFilter = category,
                Categories = categories,
                Products = products
            };

            return View(vm);
        }

        // POST: /RoomService/AddCharge
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCharge(RoomServiceOrderVm order)
        {
            var booking = await _db.Bookings
                .Include(b => b.PaymentEntries)
                .FirstOrDefaultAsync(b => b.Id == order.BookingId);

            if (booking == null) return NotFound();

            var product = await _db.Products.FindAsync(order.ProductId);
            if (product == null) return NotFound();

            if (order.Quantity < 1) order.Quantity = 1;
            if (order.Quantity > 50) order.Quantity = 50;

            var totalAmount = product.Price * order.Quantity;
            var user = await _users.GetUserAsync(User);

            var description = order.Quantity > 1
                ? $"Room Service: {product.Name} x{order.Quantity}"
                : $"Room Service: {product.Name}";

            if (!string.IsNullOrWhiteSpace(order.Note))
                description += $" ({order.Note.Trim()})";

            var entry = new PaymentEntry
            {
                BookingId = booking.Id,
                Type = PaymentEntryType.Charge,
                Method = PaymentMethod.ManualAdjustment,
                Description = description,
                Amount = totalAmount,
                Reference = $"RS-{product.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                ProcessedBy = user?.Email ?? User.Identity?.Name,
                ProcessedAt = DateTime.UtcNow
            };

            _db.PaymentEntries.Add(entry);
            await _db.SaveChangesAsync();

            await _audit.LogAsync("RoomService.Charge", "PaymentEntry", entry.Id,
                $"Booking #{booking.Id}, {product.Name} x{order.Quantity} = {totalAmount:C}", User);

            TempData["PaymentOk"] = $"Room service charge of {totalAmount:C} added ({product.Name} x{order.Quantity}).";
            return RedirectToAction(nameof(Order), new { bookingId = booking.Id });
        }
    }
}
