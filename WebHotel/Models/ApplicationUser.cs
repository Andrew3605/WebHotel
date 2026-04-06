using Microsoft.AspNetCore.Identity;

namespace WebHotel.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? CustomerId { get; set; }   // link to your Customers table
    }
}
