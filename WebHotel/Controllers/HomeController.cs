using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.ViewModels;

namespace WebHotel.Controllers
{
    [Authorize]   // 👈 add this line
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
