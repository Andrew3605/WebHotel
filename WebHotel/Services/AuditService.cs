using System.Security.Claims;
using WebHotel.Data;
using WebHotel.Models;

namespace WebHotel.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityType, int? entityId, string? details, ClaimsPrincipal user);
    }

    public class AuditService : IAuditService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AuditService> _logger;

        public AuditService(AppDbContext db, ILogger<AuditService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task LogAsync(string action, string entityType, int? entityId, string? details, ClaimsPrincipal user)
        {
            var email = user.FindFirstValue(ClaimTypes.Email)
                ?? user.FindFirstValue(ClaimTypes.Name)
                ?? "unknown";

            var role = user.IsInRole("Admin") ? "Admin"
                : user.IsInRole("Staff") ? "Staff"
                : "Customer";

            var entry = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details?.Length > 500 ? details[..500] : details,
                PerformedBy = email,
                UserRole = role,
                PerformedAt = DateTime.UtcNow
            };

            _db.AuditLogs.Add(entry);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Audit: {Action} on {EntityType} #{EntityId} by {User} ({Role})",
                action, entityType, entityId, email, role);
        }
    }
}
