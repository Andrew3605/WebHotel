using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebHotel.Models;

namespace WebHotel.Data
{
    // MUST inherit IdentityDbContext<ApplicationUser>
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<PaymentEntry> PaymentEntries => Set<PaymentEntry>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<CustomerRequest> CustomerRequests => Set<CustomerRequest>();

    }
}
