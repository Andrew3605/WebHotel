using System.ComponentModel.DataAnnotations;

namespace WebHotel.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string FullName { get; set; } = "";

        [Required, EmailAddress, StringLength(120)]
        public string Email { get; set; } = "";

        [Phone, StringLength(40)]
        public string? Phone { get; set; }
    }
}
