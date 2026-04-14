using Microsoft.AspNetCore.Mvc;
using WebHotel.Controllers.Api;
using WebHotel.Dtos;
using WebHotel.Tests.Helpers;
using Xunit;

namespace WebHotel.Tests.Api
{
    public class CustomersApiTests
    {
        [Fact]
        public async Task GetAll_ReturnsAllCustomers()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedCustomers(db);
            var controller = new CustomersApiController(db);

            var result = await controller.GetAll(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var customers = Assert.IsAssignableFrom<IEnumerable<CustomerDto>>(ok.Value);
            Assert.Equal(2, customers.Count());
        }

        [Fact]
        public async Task GetAll_WithSearch_FiltersResults()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedCustomers(db);
            var controller = new CustomersApiController(db);

            var result = await controller.GetAll("alice");

            var ok = Assert.IsType<OkObjectResult>(result);
            var customers = Assert.IsAssignableFrom<IEnumerable<CustomerDto>>(ok.Value);
            Assert.Single(customers);
            Assert.Equal("Alice Smith", customers.First().FullName);
        }

        [Fact]
        public async Task Get_ValidId_ReturnsCustomer()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedCustomers(db);
            var controller = new CustomersApiController(db);

            var result = await controller.Get(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var customer = Assert.IsType<CustomerDto>(ok.Value);
            Assert.Equal("alice@test.com", customer.Email);
        }

        [Fact]
        public async Task Get_InvalidId_ReturnsNotFound()
        {
            using var db = TestDbHelper.CreateContext();
            var controller = new CustomersApiController(db);

            var result = await controller.Get(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_ValidCustomer_ReturnsCreated()
        {
            using var db = TestDbHelper.CreateContext();
            var controller = new CustomersApiController(db);

            var dto = new CreateCustomerDto("Charlie Brown", "charlie@test.com", "023333333");
            var result = await controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var customer = Assert.IsType<CustomerDto>(created.Value);
            Assert.Equal("Charlie Brown", customer.FullName);
            Assert.Equal(1, db.Customers.Count());
        }

        [Fact]
        public async Task Update_ValidId_UpdatesCustomer()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedCustomers(db);
            var controller = new CustomersApiController(db);

            var dto = new UpdateCustomerDto("Alice Updated", "alice.new@test.com", "029999999");
            var result = await controller.Update(1, dto);

            Assert.IsType<NoContentResult>(result);
            var updated = db.Customers.Find(1)!;
            Assert.Equal("Alice Updated", updated.FullName);
        }

        [Fact]
        public async Task Update_InvalidId_ReturnsNotFound()
        {
            using var db = TestDbHelper.CreateContext();
            var controller = new CustomersApiController(db);

            var dto = new UpdateCustomerDto("Nobody", "nobody@test.com", null);
            var result = await controller.Update(999, dto);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ValidId_RemovesCustomer()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedCustomers(db);
            var controller = new CustomersApiController(db);

            var result = await controller.Delete(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Single(db.Customers);
        }
    }
}
