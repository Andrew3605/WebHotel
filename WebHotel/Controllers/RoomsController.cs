using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.Models;

namespace WebHotel.Controllers
{
    [Authorize(Roles = "Admin")] // 👈 whole controller is Admin-only
    public class RoomsController : Controller
    {
        private readonly AppDbContext _context;
        public RoomsController(AppDbContext context) => _context = context;

        // REMOVE [AllowAnonymous] here
        public async Task<IActionResult> Index()
            => View(await _context.Rooms.ToListAsync());

        // REMOVE [AllowAnonymous] here
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.FirstOrDefaultAsync(m => m.Id == id);
            if (room == null) return NotFound();
            return View(room);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Number,Type,PricePerNight,Capacity,Description,ImageUrl")] Room room)
        {
            if (!ModelState.IsValid) return View(room);
            _context.Add(room);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            return View(room);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Number,Type,PricePerNight,Capacity,Description,ImageUrl")] Room room)
        {
            if (id != room.Id) return NotFound();
            if (!ModelState.IsValid) return View(room);

            try { _context.Update(room); await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Rooms.Any(e => e.Id == room.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.FirstOrDefaultAsync(m => m.Id == id);
            if (room == null) return NotFound();
            return View(room);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null) _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
