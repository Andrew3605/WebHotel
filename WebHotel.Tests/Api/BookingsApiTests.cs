using Microsoft.AspNetCore.Mvc;
using WebHotel.Controllers.Api;
using WebHotel.Dtos;
using WebHotel.Tests.Helpers;
using Xunit;

namespace WebHotel.Tests.Api
{
    public class BookingsApiTests
    {
        [Fact]
        public async Task GetAll_ReturnsAllBookings()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedAll(db);
            var controller = new BookingsApiController(db);

            var result = await controller.GetAll(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var bookings = Assert.IsAssignableFrom<IEnumerable<BookingDto>>(ok.Value);
            Assert.Equal(2, bookings.Count());
        }

        [Fact]
        public async Task Get_ValidId_ReturnsBooking()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedAll(db);
            var controller = new BookingsApiController(db);

            var result = await controller.Get(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var booking = Assert.IsType<BookingDto>(ok.Value);
            Assert.Equal(1, booking.CustomerId);
            Assert.Equal(450m, booking.TotalPrice);
        }

        [Fact]
        public async Task Get_InvalidId_ReturnsNotFound()
        {
            using var db = TestDbHelper.CreateContext();
            var controller = new BookingsApiController(db);

            var result = await controller.Get(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_ValidBooking_ReturnsCreated()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedRooms(db);
            TestDbHelper.SeedCustomers(db);
            var controller = new BookingsApiController(db);

            var dto = new CreateBookingDto(1, 3,
                new DateTime(2026, 7, 1), new DateTime(2026, 7, 5));
            var result = await controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var booking = Assert.IsType<BookingDto>(created.Value);
            Assert.Equal(400m, booking.TotalPrice); // 4 nights * $100
        }

        [Fact]
        public async Task Create_ConflictingDates_ReturnsBadRequest()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedAll(db);
            var controller = new BookingsApiController(db);

            // Room 1 is booked May 1-4
            var dto = new CreateBookingDto(2, 1,
                new DateTime(2026, 5, 2), new DateTime(2026, 5, 6));
            var result = await controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_InvalidCheckOutBeforeCheckIn_ReturnsBadRequest()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedAll(db);
            var controller = new BookingsApiController(db);

            var dto = new CreateBookingDto(1, 1,
                new DateTime(2026, 8, 5), new DateTime(2026, 8, 3));
            var result = await controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_NonExistentRoom_ReturnsBadRequest()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedCustomers(db);
            var controller = new BookingsApiController(db);

            var dto = new CreateBookingDto(1, 999,
                new DateTime(2026, 9, 1), new DateTime(2026, 9, 3));
            var result = await controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_NonExistentCustomer_ReturnsBadRequest()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedRooms(db);
            var controller = new BookingsApiController(db);

            var dto = new CreateBookingDto(999, 1,
                new DateTime(2026, 9, 1), new DateTime(2026, 9, 3));
            var result = await controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_ValidBooking_ReturnsNoContent()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedAll(db);
            var controller = new BookingsApiController(db);

            var dto = new UpdateBookingDto(1, 1,
                new DateTime(2026, 5, 1), new DateTime(2026, 5, 6), false);
            var result = await controller.Update(1, dto);

            Assert.IsType<NoContentResult>(result);
            var updated = db.Bookings.Find(1)!;
            Assert.Equal(750m, updated.TotalPrice); // 5 nights * $150
        }

        [Fact]
        public async Task Delete_ValidId_RemovesBooking()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedAll(db);
            var controller = new BookingsApiController(db);

            var result = await controller.Delete(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Single(db.Bookings);
        }

        [Fact]
        public async Task Delete_InvalidId_ReturnsNotFound()
        {
            using var db = TestDbHelper.CreateContext();
            var controller = new BookingsApiController(db);

            var result = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
