using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.Models;
using WebHotel.Services;
using WebHotel.ViewModels;

namespace WebHotel.Controllers
{
    // Customers: My() page only; Admin + Staff: full booking management
    [Authorize(Roles = "Customer,Admin,Staff")]
    public class BookingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;

        public BookingsController(AppDbContext context, UserManager<ApplicationUser> userManager, IAuditService audit)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
        }

        // ---------------- CUSTOMER PAGE ----------------
        // GET: /Bookings/My  -> a customer sees ONLY their own bookings
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> My()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CustomerId == null) return Forbid();

            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.PaymentEntries)
                .Where(b => b.CustomerId == user.CustomerId)
                .OrderByDescending(b => b.CheckIn)
                .ToListAsync();

            return View(bookings);
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Book(int roomId, DateTime checkIn, DateTime checkOut)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return NotFound();

            var model = new Booking
            {
                RoomId = room.Id,
                CheckIn = checkIn.Date,
                CheckOut = checkOut.Date
            };
            ViewData["RoomNumber"] = room.Number;
            return View(model); // Views/Bookings/Book.cshtml
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Book([Bind("RoomId,CheckIn,CheckOut")] Booking booking)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CustomerId == null) return Forbid();
            booking.CustomerId = user.CustomerId.Value;

            if (booking.CheckOut.Date <= booking.CheckIn.Date)
                ModelState.AddModelError(string.Empty, "Check-out must be after check-in.");

            bool conflict = await _context.Bookings.AnyAsync(b =>
                b.RoomId == booking.RoomId &&
                !(b.CheckOut <= booking.CheckIn || b.CheckIn >= booking.CheckOut));

            if (conflict)
                ModelState.AddModelError(string.Empty, "Room is not available for those dates.");

            if (!ModelState.IsValid)
            {
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                ViewData["RoomNumber"] = room?.Number;
                return View(booking);
            }

            var r = await _context.Rooms.FindAsync(booking.RoomId);
            var nights = Math.Max(1, (booking.CheckOut.Date - booking.CheckIn.Date).Days);
            booking.TotalPrice = (r?.PricePerNight ?? 0m) * nights;
            booking.CreatedAt = DateTime.UtcNow;
            booking.IsPaid = false;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(My));
        }
        // ------------------------------------------------

        // ---------------- ADMIN PAGES -------------------

        // GET: Bookings?search=foo&page=2
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            const int pageSize = 10;

            IQueryable<Booking> query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Include(b => b.PaymentEntries);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                var normalized = search.ToLower();

                query = query.Where(b =>
                    b.Id.ToString().Contains(search) ||
                    b.RoomId.ToString().Contains(search) ||
                    b.CustomerId.ToString().Contains(search) ||
                    (b.Customer != null && (
                        b.Customer.Email.ToLower().Contains(normalized) ||
                        b.Customer.FullName.ToLower().Contains(normalized))) ||
                    (b.Room != null && (
                        b.Room.Number.ToLower().Contains(normalized) ||
                        b.Room.Type.ToLower().Contains(normalized))) ||
                    b.CheckIn.ToString().Contains(search) ||
                    b.CheckOut.ToString().Contains(search) ||
                    (normalized == "paid" && b.IsPaid) ||
                    (normalized == "unpaid" && !b.IsPaid));
            }

            ViewData["CurrentSearch"] = search;
            var ordered = query.OrderByDescending(b => b.CheckIn).ThenByDescending(b => b.Id);
            var paginated = await PaginatedList<Booking>.CreateAsync(ordered, page, pageSize);
            return View(paginated);
        }

        // (Optional) Details page if you have the view
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Include(b => b.PaymentEntries)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();
            return View(booking);
        }

        // GET: Bookings/Create
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Create(int? roomId, DateTime? checkIn, DateTime? checkOut, int? customerId)
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", customerId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Number", roomId);

            var model = new Booking
            {
                RoomId = roomId ?? 0,
                CustomerId = customerId ?? 0,
                CheckIn = (checkIn ?? DateTime.Today.AddDays(1)).Date,
                CheckOut = (checkOut ?? DateTime.Today.AddDays(2)).Date
            };
            return View(model);
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create([Bind("Id,CustomerId,RoomId,CheckIn,CheckOut,TotalPrice,IsPaid,CreatedAt")] Booking booking)
        {
            if (booking.CheckOut.Date <= booking.CheckIn.Date)
                ModelState.AddModelError(string.Empty, "Check-out must be after check-in.");

            bool conflict = await _context.Bookings.AnyAsync(b =>
                b.RoomId == booking.RoomId &&
                !(b.CheckOut <= booking.CheckIn || b.CheckIn >= booking.CheckOut));

            if (conflict)
                ModelState.AddModelError(string.Empty, "Room is not available for those dates.");

            if (ModelState.IsValid)
            {
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                var nights = Math.Max(1, (booking.CheckOut.Date - booking.CheckIn.Date).Days);
                booking.TotalPrice = (room?.PricePerNight ?? 0m) * nights;
                booking.CreatedAt = DateTime.UtcNow;

                _context.Add(booking);
                await _context.SaveChangesAsync();
                await _audit.LogAsync("Booking.Create", "Booking", booking.Id,
                    $"Room {booking.RoomId}, Customer {booking.CustomerId}, {booking.CheckIn:d}-{booking.CheckOut:d}", User);
                return RedirectToAction(nameof(Index));
            }

            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", booking.CustomerId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Number", booking.RoomId);
            return View(booking);
        }

        // GET: Bookings/Edit/5
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", booking.CustomerId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Number", booking.RoomId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerId,RoomId,CheckIn,CheckOut,TotalPrice,IsPaid,CreatedAt,RowVersion")] Booking booking)
        {
            if (id != booking.Id) return NotFound();

            if (booking.CheckOut.Date <= booking.CheckIn.Date)
                ModelState.AddModelError(string.Empty, "Check-out must be after check-in.");

            bool conflict = await _context.Bookings.AnyAsync(b =>
                b.RoomId == booking.RoomId && b.Id != booking.Id &&
                !(b.CheckOut <= booking.CheckIn || b.CheckIn >= booking.CheckOut));

            if (conflict)
                ModelState.AddModelError(string.Empty, "Room is not available for those dates.");

            if (ModelState.IsValid)
            {
                try
                {
                    var room = await _context.Rooms.FindAsync(booking.RoomId);
                    var nights = Math.Max(1, (booking.CheckOut.Date - booking.CheckIn.Date).Days);
                    booking.TotalPrice = (room?.PricePerNight ?? 0m) * nights;

                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    await _audit.LogAsync("Booking.Edit", "Booking", booking.Id,
                        $"Room {booking.RoomId}, {booking.CheckIn:d}-{booking.CheckOut:d}, Total {booking.TotalPrice:C}", User);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Bookings.AnyAsync(e => e.Id == booking.Id)) return NotFound();
                    ModelState.AddModelError(string.Empty,
                        "This booking was modified by another user. Please reload and try again.");
                }
            }

            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", booking.CustomerId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Number", booking.RoomId);
            return View(booking);
        }

        // GET: Bookings/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Include(b => b.PaymentEntries)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (booking == null) return NotFound();

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                await _audit.LogAsync("Booking.Delete", "Booking", id,
                    $"Room {booking.RoomId}, Customer {booking.CustomerId}", User);
                _context.Bookings.Remove(booking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // -------------------------------------------------

        private bool BookingExists(int id) => _context.Bookings.Any(e => e.Id == id);
    }
}
