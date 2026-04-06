using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WebHotel.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required, StringLength(10)]
        public string Number { get; set; } = "";   // e.g., "305"

        [Required, StringLength(40)]
        public string Type { get; set; } = "Deluxe"; // Deluxe / Twin / Suite

        [Precision(18, 2)]
        public decimal PricePerNight { get; set; }

        [Range(1, 10)]
        public int Capacity { get; set; } = 2;

        [StringLength(500)]
        public string? Description { get; set; }

        [Url]
        public string? ImageUrl { get; set; }
    }
}
