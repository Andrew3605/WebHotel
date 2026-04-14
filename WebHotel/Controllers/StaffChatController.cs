using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.Models;

namespace WebHotel.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class StaffChatController : Controller
    {
        private readonly AppDbContext _db;

        public StaffChatController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /StaffChat — dashboard showing waiting + active chats
        public async Task<IActionResult> Index()
        {
            var sessions = await _db.ChatSessions
                .Include(s => s.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(s => s.Status == ChatSessionStatus.Waiting || s.Status == ChatSessionStatus.Active)
                .OrderByDescending(s => s.Status == ChatSessionStatus.Waiting ? 1 : 0) // Waiting first
                .ThenByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(sessions);
        }

        // GET: /StaffChat/Chat/5 — staff chat room for a session
        public async Task<IActionResult> Chat(int id)
        {
            var session = await _db.ChatSessions
                .Include(s => s.Messages.OrderBy(m => m.SentAt))
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return NotFound();
            if (session.Status == ChatSessionStatus.Bot) return NotFound(); // Can't join bot sessions

            return View(session);
        }

        // GET: /StaffChat/History — view closed chats
        public async Task<IActionResult> History(int page = 1)
        {
            const int pageSize = 20;
            var query = _db.ChatSessions
                .Include(s => s.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(s => s.Status == ChatSessionStatus.Closed)
                .OrderByDescending(s => s.ClosedAt);

            var total = await query.CountAsync();
            var sessions = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewData["Page"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);
            return View(sessions);
        }
    }
}
