using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.Models;

namespace WebHotel.Tests.Helpers
{
    public static class TestDbHelper
    {
        public static AppDbContext CreateContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        public static void SeedRooms(AppDbContext db)
        {
            db.Rooms.AddRange(
                new Room { Id = 1, Number = "101", Type = "Deluxe", PricePerNight = 150m, Capacity = 2, Description = "Ocean view" },
                new Room { Id = 2, Number = "202", Type = "Suite", PricePerNight = 300m, Capacity = 4, Description = "Penthouse" },
                new Room { Id = 3, Number = "303", Type = "Twin", PricePerNight = 100m, Capacity = 2, Description = "City view" }
            );
            db.SaveChanges();
        }

        public static void SeedCustomers(AppDbContext db)
        {
            db.Customers.AddRange(
                new Customer { Id = 1, FullName = "Alice Smith", Email = "alice@test.com", Phone = "021111111" },
                new Customer { Id = 2, FullName = "Bob Jones", Email = "bob@test.com", Phone = "022222222" }
            );
            db.SaveChanges();
        }

        public static void SeedBookings(AppDbContext db)
        {
            db.Bookings.AddRange(
                new Booking
                {
                    Id = 1, CustomerId = 1, RoomId = 1,
                    CheckIn = new DateTime(2026, 5, 1), CheckOut = new DateTime(2026, 5, 4),
                    TotalPrice = 450m, CreatedAt = DateTime.UtcNow
                },
                new Booking
                {
                    Id = 2, CustomerId = 2, RoomId = 2,
                    CheckIn = new DateTime(2026, 6, 10), CheckOut = new DateTime(2026, 6, 12),
                    TotalPrice = 600m, CreatedAt = DateTime.UtcNow
                }
            );
            db.SaveChanges();
        }

        public static void SeedAll(AppDbContext db)
        {
            SeedRooms(db);
            SeedCustomers(db);
            SeedBookings(db);
        }
    }
}
