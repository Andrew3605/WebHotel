using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WebHotel.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = "";

        [Required, StringLength(40)]
        public string Category { get; set; } = "";

        [Range(0, 100000)]
        [Precision(18, 2)]
        public decimal Price { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Url]
        public string? ImageUrl { get; set; }
    }
}
