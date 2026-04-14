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

namespace WebHotel.Controllers
{
    [Authorize] // must be signed in for anything here
    public class CustomerRequestsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IHotelEmailSender _email;

        public CustomerRequestsController(AppDbContext db, UserManager<ApplicationUser> users, IHotelEmailSender email)
        {
            _db = db;
            _users = users;
            _email = email;
        }

        // =============== CUSTOMER ===============

        // GET: /CustomerRequests/Create?roomId=1&checkIn=2025-10-25&checkOut=2025-10-27
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(int? roomId, DateTime? checkIn, DateTime? checkOut)
        {
            var roomItems = await _db.Rooms
                .OrderBy(r => r.Number)
                .Select(r => new { r.Id, Label = r.Number })   // shows “Room A”, “Room B” etc.
                .ToListAsync();

            ViewBag.Rooms = new SelectList(roomItems, "Id", "Label", roomId);
            ViewBag.PrefillCheckIn = (checkIn ?? DateTime.Today.AddDays(1)).ToString("yyyy-MM-dd");
            ViewBag.PrefillCheckOut = (checkOut ?? DateTime.Today.AddDays(2)).ToString("yyyy-MM-dd");

            return View(new CustomerRequest
            {
                Type = RequestType.NewBooking,
                RoomId = roomId,
                CheckIn = checkIn,
                CheckOut = checkOut
            });
        }

        // POST: /CustomerRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([Bind("Type,RoomId,BookingId,CheckIn,CheckOut,Message")] CustomerRequest r)
        {
            var u = await _users.GetUserAsync(User);
            if (u?.CustomerId == null)
                return Forbid();

            r.CustomerId = u.CustomerId;
            r.Status = RequestStatus.Pending;
            r.CreatedAt = DateTime.UtcNow;
            r.Message = string.IsNullOrWhiteSpace(r.Message) ? null : r.Message.Trim();

            if (r.Message != null && r.Message.Length > 500)
                ModelState.AddModelError(nameof(r.Message), "Notes cannot exceed 500 characters.");

            if (r.Type == RequestType.NewBooking)
            {
                if (r.RoomId == null || r.CheckIn == null || r.CheckOut == null || r.CheckOut <= r.CheckIn)
                    ModelState.AddModelError(string.Empty, "Please choose a room and valid dates.");
            }

            var pendingCount = await _db.CustomerRequests
                .CountAsync(x => x.CustomerId == r.CustomerId && x.Status == RequestStatus.Pending);

            if (pendingCount >= 5)
                ModelState.AddModelError(string.Empty, "You already have 5 pending requests. Please wait for staff to review them before submitting more.");

            var duplicateWindowStart = DateTime.UtcNow.AddMinutes(-10);
            var normalizedMessage = r.Message?.ToLower();

            var hasRecentDuplicate = await _db.CustomerRequests.AnyAsync(x =>
                x.CustomerId == r.CustomerId &&
                x.Status == RequestStatus.Pending &&
                x.CreatedAt >= duplicateWindowStart &&
                x.Type == r.Type &&
                x.RoomId == r.RoomId &&
                x.BookingId == r.BookingId &&
                x.CheckIn == r.CheckIn &&
                x.CheckOut == r.CheckOut &&
                ((x.Message == null && r.Message == null) ||
                 (x.Message != null && normalizedMessage != null && x.Message.ToLower() == normalizedMessage)));

            if (hasRecentDuplicate)
                ModelState.AddModelError(string.Empty, "A very similar request was already submitted recently. Please wait a few minutes before trying again.");

            if (!ModelState.IsValid)
            {
                var roomItems = await _db.Rooms
                    .OrderBy(x => x.Number)
                    .Select(x => new { x.Id, Label = x.Number })
                    .ToListAsync();
                ViewBag.Rooms = new SelectList(roomItems, "Id", "Label", r.RoomId);
                return View(r);
            }

            _db.CustomerRequests.Add(r);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(My));
        }

        // GET: /CustomerRequests/My — customer sees their own requests
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> My()
        {
            var u = await _users.GetUserAsync(User);
            if (u?.CustomerId == null)
                return Forbid();

            var cid = u.CustomerId;

            var list = await _db.CustomerRequests
                .Where(x => x.CustomerId == cid)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // ================ ADMIN =================

        // GET: /CustomerRequests/Admin   (optionally filter by status/search)
        // e.g. /CustomerRequests/Admin?status=Pending&search=101
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin(RequestStatus? status = null, string? search = null)
        {
            var q = _db.CustomerRequests.AsQueryable();
            if (status.HasValue) q = q.Where(r => r.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                var normalized = term.ToLower();

                q = q.Where(r =>
                    (r.Message != null && r.Message.ToLower().Contains(normalized)) ||
                    r.Id.ToString().Contains(term) ||
                    (r.CustomerId.HasValue && r.CustomerId.Value.ToString().Contains(term)) ||
                    (r.RoomId.HasValue && r.RoomId.Value.ToString().Contains(term)) ||
                    r.Type.ToString().ToLower().Contains(normalized) ||
                    r.Status.ToString().ToLower().Contains(normalized));
            }

            var list = await q
                .OrderBy(r => r.Status)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            ViewBag.Search = search;
            return View(list);
        }

        // POST: /CustomerRequests/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var r = await _db.CustomerRequests.FindAsync(id);
            if (r == null) return NotFound();

            switch (r.Type)
            {
                case RequestType.NewBooking:
                    if (!await ApproveNewBooking(r)) return RedirectToAction(nameof(Admin));
                    break;

                case RequestType.ExtendStay:
                    if (!await ApproveExtendStay(r)) return RedirectToAction(nameof(Admin));
                    break;

                case RequestType.ChangeRoom:
                    if (!await ApproveChangeRoom(r)) return RedirectToAction(nameof(Admin));
                    break;

                case RequestType.EarlyCheckout:
                    if (!await ApproveEarlyCheckout(r)) return RedirectToAction(nameof(Admin));
                    break;

                // OrderFood — no automated action, just status change
                default:
                    break;
            }

            r.Status = RequestStatus.Approved;
            await _db.SaveChangesAsync();

            // Send email notification
            if (r.CustomerId.HasValue)
            {
                var customer = await _db.Customers.FindAsync(r.CustomerId.Value);
                if (customer != null)
                {
                    await _email.SendRequestStatusAsync(customer.Email, customer.FullName,
                        r.Type.ToString(), "Approved");

                    // Send booking confirmation for new bookings
                    if (r.Type == RequestType.NewBooking && r.RoomId.HasValue)
                    {
                        var roomForEmail = await _db.Rooms.FindAsync(r.RoomId.Value);
                        var booking = await _db.Bookings
                            .OrderByDescending(b => b.CreatedAt)
                            .FirstOrDefaultAsync(b => b.CustomerId == r.CustomerId && b.RoomId == r.RoomId);
                        if (booking != null && roomForEmail != null)
                            await _email.SendBookingConfirmationAsync(customer.Email, customer.FullName,
                                booking.Id, roomForEmail.Number, booking.CheckIn, booking.CheckOut, booking.TotalPrice);
                    }
                }
            }

            return RedirectToAction(nameof(Admin));
        }

        // POST: /CustomerRequests/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var r = await _db.CustomerRequests.FindAsync(id);
            if (r == null) return NotFound();

            r.Status = RequestStatus.Rejected;
            await _db.SaveChangesAsync();

            // Send email notification
            if (r.CustomerId.HasValue)
            {
                var customer = await _db.Customers.FindAsync(r.CustomerId.Value);
                if (customer != null)
                    await _email.SendRequestStatusAsync(customer.Email, customer.FullName,
                        r.Type.ToString(), "Rejected");
            }

            return RedirectToAction(nameof(Admin));
        }

        // POST: /CustomerRequests/Delete/5   (delete a single request)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.CustomerRequests.FindAsync(id);
            if (r == null) return NotFound();

            _db.CustomerRequests.Remove(r);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Admin));
        }

        // POST: /CustomerRequests/ClearResolved  (bulk delete history: Approved/Rejected older than N days)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ClearResolved(int days = 30)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);

            var oldResolved = await _db.CustomerRequests
                .Where(x => x.Status != RequestStatus.Pending && x.CreatedAt < cutoff)
                .ToListAsync();

            if (oldResolved.Count > 0)
            {
                _db.CustomerRequests.RemoveRange(oldResolved);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Admin));
        }

        // =============== REQUEST HANDLERS ===============

        /// <summary>Creates a new booking from the request. Returns false if conflict (auto-rejects).</summary>
        private async Task<bool> ApproveNewBooking(CustomerRequest r)
        {
            if (!r.RoomId.HasValue || !r.CheckIn.HasValue || !r.CheckOut.HasValue)
                return true; // nothing to do, just approve status

            bool conflict = await _db.Bookings.AnyAsync(b =>
                b.RoomId == r.RoomId.Value &&
                !(b.CheckOut <= r.CheckIn.Value || b.CheckIn >= r.CheckOut.Value));

            if (conflict) { r.Status = RequestStatus.Rejected; await _db.SaveChangesAsync(); return false; }

            var room = await _db.Rooms.FindAsync(r.RoomId.Value);
            var nights = Math.Max(1, (r.CheckOut.Value.Date - r.CheckIn.Value.Date).Days);

            _db.Bookings.Add(new Booking
            {
                CustomerId = r.CustomerId ?? 0,
                RoomId = r.RoomId.Value,
                CheckIn = r.CheckIn.Value.Date,
                CheckOut = r.CheckOut.Value.Date,
                TotalPrice = (room?.PricePerNight ?? 0m) * nights,
                CreatedAt = DateTime.UtcNow
            });
            return true;
        }

        /// <summary>Extends the checkout date and recalculates the total price.</summary>
        private async Task<bool> ApproveExtendStay(CustomerRequest r)
        {
            if (!r.BookingId.HasValue || !r.CheckOut.HasValue) return true;

            var booking = await _db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == r.BookingId.Value);
            if (booking == null) return true;

            var newCheckOut = r.CheckOut.Value.Date;
            if (newCheckOut <= booking.CheckOut) return true; // not actually extending

            bool conflict = await _db.Bookings.AnyAsync(b =>
                b.RoomId == booking.RoomId && b.Id != booking.Id &&
                !(b.CheckOut <= booking.CheckIn || b.CheckIn >= newCheckOut));

            if (conflict) { r.Status = RequestStatus.Rejected; await _db.SaveChangesAsync(); return false; }

            booking.CheckOut = newCheckOut;
            var nights = Math.Max(1, (booking.CheckOut.Date - booking.CheckIn.Date).Days);
            booking.TotalPrice = (booking.Room?.PricePerNight ?? 0m) * nights;
            return true;
        }

        /// <summary>Moves a booking to a different room and recalculates the price.</summary>
        private async Task<bool> ApproveChangeRoom(CustomerRequest r)
        {
            if (!r.BookingId.HasValue || !r.RoomId.HasValue) return true;

            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == r.BookingId.Value);
            if (booking == null) return true;

            var newRoom = await _db.Rooms.FindAsync(r.RoomId.Value);
            if (newRoom == null) return true;

            bool conflict = await _db.Bookings.AnyAsync(b =>
                b.RoomId == r.RoomId.Value && b.Id != booking.Id &&
                !(b.CheckOut <= booking.CheckIn || b.CheckIn >= booking.CheckOut));

            if (conflict) { r.Status = RequestStatus.Rejected; await _db.SaveChangesAsync(); return false; }

            booking.RoomId = newRoom.Id;
            var nights = Math.Max(1, (booking.CheckOut.Date - booking.CheckIn.Date).Days);
            booking.TotalPrice = newRoom.PricePerNight * nights;
            return true;
        }

        /// <summary>Moves the checkout date earlier (today or request date) and recalculates.</summary>
        private async Task<bool> ApproveEarlyCheckout(CustomerRequest r)
        {
            if (!r.BookingId.HasValue) return true;

            var booking = await _db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == r.BookingId.Value);
            if (booking == null) return true;

            var newCheckOut = r.CheckOut?.Date ?? DateTime.UtcNow.Date;
            if (newCheckOut <= booking.CheckIn) newCheckOut = booking.CheckIn.AddDays(1); // at least 1 night

            booking.CheckOut = newCheckOut;
            var nights = Math.Max(1, (booking.CheckOut.Date - booking.CheckIn.Date).Days);
            booking.TotalPrice = (booking.Room?.PricePerNight ?? 0m) * nights;
            return true;
        }
    }
}
