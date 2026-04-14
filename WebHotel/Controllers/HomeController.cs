using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.Models;
using WebHotel.Services;
using WebHotel.ViewModels;

namespace WebHotel.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) { _db = db; }

        // GET: /Home/Index
        [HttpGet]
        public IActionResult Index()
        {
            var vm = new AvailabilitySearchVm
            {
                CheckIn = DateTime.Today.AddDays(1),
                CheckOut = DateTime.Today.AddDays(2),
                Guests = 2
            };
            return View(vm);
        }

        // GET: /Home/Dashboard (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.UtcNow.Date;
            var sixMonthsAgo = today.AddMonths(-5).Date;
            var firstOfRange = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

            var bookings = await _db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Include(b => b.PaymentEntries)
                .ToListAsync();

            var totalRooms = await _db.Rooms.CountAsync();
            var totalCustomers = await _db.Customers.CountAsync();
            var pendingRequests = await _db.CustomerRequests
                .CountAsync(r => r.Status == RequestStatus.Pending);

            var occupiedToday = bookings.Count(b => b.CheckIn <= today && b.CheckOut > today);

            // Bookings per month (last 6 months)
            var months = new List<string>();
            var counts = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                var month = firstOfRange.AddMonths(i);
                var endOfMonth = month.AddMonths(1);
                months.Add(month.ToString("MMM yyyy"));
                counts.Add(bookings.Count(b => b.CreatedAt >= month && b.CreatedAt < endOfMonth));
            }

            // Revenue by room type
            var revenueByType = bookings
                .Where(b => b.Room != null)
                .GroupBy(b => b.Room!.Type)
                .Select(g => new { Type = g.Key, Revenue = g.Sum(b => BookingPaymentCalculator.GetPaymentsReceived(b.PaymentEntries)) })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            var totalRevenue = bookings.Sum(b => BookingPaymentCalculator.GetPaymentsReceived(b.PaymentEntries));
            var outstandingBalance = bookings.Sum(b => BookingPaymentCalculator.GetBalanceDue(b.TotalPrice, b.PaymentEntries));

            var recent = bookings
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new RecentBookingVm
                {
                    Id = b.Id,
                    CustomerName = b.Customer?.FullName ?? "N/A",
                    RoomNumber = b.Room?.Number ?? "N/A",
                    CheckIn = b.CheckIn,
                    CheckOut = b.CheckOut,
                    TotalPrice = b.TotalPrice,
                    IsPaid = BookingPaymentCalculator.IsFullyPaid(b.TotalPrice, b.PaymentEntries)
                })
                .ToList();

            var vm = new AdminDashboardVm
            {
                TotalRooms = totalRooms,
                TotalCustomers = totalCustomers,
                TotalBookings = bookings.Count,
                PendingRequests = pendingRequests,
                TotalRevenue = totalRevenue,
                OutstandingBalance = Math.Max(0, outstandingBalance),
                RoomsOccupiedToday = occupiedToday,
                OccupancyRate = totalRooms > 0 ? Math.Round((decimal)occupiedToday / totalRooms * 100, 1) : 0,
                BookingMonths = months,
                BookingCounts = counts,
                RoomTypes = revenueByType.Select(x => x.Type).ToList(),
                RevenueByType = revenueByType.Select(x => x.Revenue).ToList(),
                RecentBookings = recent
            };

            return View(vm);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        // POST: /Home/Search
        [HttpPost]
        public async Task<IActionResult> Search(AvailabilitySearchVm vm)
        {
            if (vm.CheckOut.Date <= vm.CheckIn.Date)
            {
                ModelState.AddModelError(string.Empty, "Check-out must be after check-in.");
                return View("Index", vm);
            }

            var available = await _db.Rooms
                .Where(r => r.Capacity >= vm.Guests)
                .Where(r => !_db.Bookings.Any(b =>
                    b.RoomId == r.Id &&
                    !(b.CheckOut <= vm.CheckIn || b.CheckIn >= vm.CheckOut)))
                .OrderBy(r => r.PricePerNight)
                .ToListAsync();

            vm.AvailableRooms = available;
            return View("Index", vm);
        }
    }
}
