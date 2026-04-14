using System.ComponentModel.DataAnnotations;

namespace WebHotel.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Action { get; set; } = string.Empty;  // e.g. "Booking.Create", "Request.Approve"

        [Required, StringLength(50)]
        public string EntityType { get; set; } = string.Empty;  // e.g. "Booking", "CustomerRequest"

        public int? EntityId { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        [Required, StringLength(120)]
        public string PerformedBy { get; set; } = string.Empty;  // user email

        [StringLength(20)]
        public string? UserRole { get; set; }  // "Admin" or "Staff"

        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    }
}
