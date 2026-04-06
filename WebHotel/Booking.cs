using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WebHotel.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckIn { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckOut { get; set; }

        [Precision(18, 2)]
        public decimal TotalPrice { get; set; }

        public bool IsPaid { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
