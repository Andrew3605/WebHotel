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

namespace WebHotel.Controllers
{
    [Authorize] // must be signed in for anything here
    public class CustomerRequestsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public CustomerRequestsController(AppDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
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
            r.CustomerId = u?.CustomerId;
            r.Status = RequestStatus.Pending;
            r.CreatedAt = DateTime.UtcNow;

            if (r.Type == RequestType.NewBooking)
            {
                if (r.RoomId == null || r.CheckIn == null || r.CheckOut == null || r.CheckOut <= r.CheckIn)
                    ModelState.AddModelError(string.Empty, "Please choose a room and valid dates.");
            }

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
            var cid = u?.CustomerId;

            var list = await _db.CustomerRequests
                .Where(x => x.CustomerId == cid)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // ================ ADMIN =================

        // GET: /CustomerRequests/Admin   (optionally filter by status)
        // e.g. /CustomerRequests/Admin?status=Pending
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin(RequestStatus? status = null)
        {
            var q = _db.CustomerRequests.AsQueryable();
            if (status.HasValue) q = q.Where(r => r.Status == status.Value);

            var list = await q
                .OrderBy(r => r.Status)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // POST: /CustomerRequests/Approve/5 — for NewBooking, creates Booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var r = await _db.CustomerRequests.FindAsync(id);
            if (r == null) return NotFound();

            if (r.Type == RequestType.NewBooking && r.RoomId.HasValue && r.CheckIn.HasValue && r.CheckOut.HasValue)
            {
                // overlap check
                bool conflict = await _db.Bookings.AnyAsync(b =>
                    b.RoomId == r.RoomId.Value &&
                    !(b.CheckOut <= r.CheckIn.Value || b.CheckIn >= r.CheckOut.Value));

                if (conflict)
                {
                    r.Status = RequestStatus.Rejected;
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Admin));
                }

                var room = await _db.Rooms.FindAsync(r.RoomId.Value);
                var nights = Math.Max(1, (r.CheckOut.Value.Date - r.CheckIn.Value.Date).Days);
                var total = (room?.PricePerNight ?? 0m) * nights;

                _db.Bookings.Add(new Booking
                {
                    CustomerId = r.CustomerId ?? 0,
                    RoomId = r.RoomId.Value,
                    CheckIn = r.CheckIn.Value.Date,
                    CheckOut = r.CheckOut.Value.Date,
                    TotalPrice = total,
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            r.Status = RequestStatus.Approved;
            await _db.SaveChangesAsync();
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
    }
}
