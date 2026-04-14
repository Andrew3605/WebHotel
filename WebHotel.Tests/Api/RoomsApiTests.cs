using Microsoft.AspNetCore.Mvc;
using WebHotel.Controllers.Api;
using WebHotel.Dtos;
using WebHotel.Tests.Helpers;
using Xunit;

namespace WebHotel.Tests.Api
{
    public class RoomsApiTests
    {
        [Fact]
        public async Task GetAll_ReturnsAllRooms()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedRooms(db);
            var controller = new RoomsApiController(db);

            var result = await controller.GetAll(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var rooms = Assert.IsAssignableFrom<IEnumerable<RoomDto>>(ok.Value);
            Assert.Equal(3, rooms.Count());
        }

        [Fact]
        public async Task GetAll_WithSearch_FiltersResults()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedRooms(db);
            var controller = new RoomsApiController(db);

            var result = await controller.GetAll("suite");

            var ok = Assert.IsType<OkObjectResult>(result);
            var rooms = Assert.IsAssignableFrom<IEnumerable<RoomDto>>(ok.Value);
            Assert.Single(rooms);
            Assert.Equal("Suite", rooms.First().Type);
        }

        [Fact]
        public async Task Get_ValidId_ReturnsRoom()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedRooms(db);
            var controller = new RoomsApiController(db);

            var result = await controller.Get(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var room = Assert.IsType<RoomDto>(ok.Value);
            Assert.Equal("101", room.Number);
        }

        [Fact]
        public async Task Get_InvalidId_ReturnsNotFound()
        {
            using var db = TestDbHelper.CreateContext();
            var controller = new RoomsApiController(db);

            var result = await controller.Get(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Availability_ReturnsOnlyAvailableRooms()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedAll(db);
            var controller = new RoomsApiController(db);

            // Room 1 is booked May 1-4, so searching May 2-3 should exclude it
            var query = new AvailabilityQueryDto(
                new DateTime(2026, 5, 2), new DateTime(2026, 5, 3), 2);

            var result = await controller.Availability(query);

            var ok = Assert.IsType<OkObjectResult>(result);
            var rooms = Assert.IsAssignableFrom<IEnumerable<RoomDto>>(ok.Value);
            Assert.DoesNotContain(rooms, r => r.Number == "101");
            Assert.Contains(rooms, r => r.Number == "303");
        }

        [Fact]
        public async Task Availability_InvalidDates_ReturnsBadRequest()
        {
            using var db = TestDbHelper.CreateContext();
            var controller = new RoomsApiController(db);

            var query = new AvailabilityQueryDto(
                new DateTime(2026, 5, 5), new DateTime(2026, 5, 3), 2);

            var result = await controller.Availability(query);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_ValidRoom_ReturnsCreated()
        {
            using var db = TestDbHelper.CreateContext();
            var controller = new RoomsApiController(db);

            var dto = new CreateRoomDto("401", "Suite", 250m, 3, "New room", null);
            var result = await controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var room = Assert.IsType<RoomDto>(created.Value);
            Assert.Equal("401", room.Number);
            Assert.Equal(1, db.Rooms.Count());
        }

        [Fact]
        public async Task Update_ValidId_UpdatesRoom()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedRooms(db);
            var controller = new RoomsApiController(db);

            var dto = new UpdateRoomDto("101-A", "Premium", 200m, 3, "Renovated", null);
            var result = await controller.Update(1, dto);

            Assert.IsType<NoContentResult>(result);
            var updated = db.Rooms.Find(1)!;
            Assert.Equal("101-A", updated.Number);
            Assert.Equal(200m, updated.PricePerNight);
        }

        [Fact]
        public async Task Delete_ValidId_RemovesRoom()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedRooms(db);
            var controller = new RoomsApiController(db);

            var result = await controller.Delete(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(2, db.Rooms.Count());
        }

        [Fact]
        public async Task Delete_InvalidId_ReturnsNotFound()
        {
            using var db = TestDbHelper.CreateContext();
            var controller = new RoomsApiController(db);

            var result = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
