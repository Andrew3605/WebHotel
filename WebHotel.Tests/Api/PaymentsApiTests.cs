using Microsoft.AspNetCore.Mvc;
using WebHotel.Controllers.Api;
using WebHotel.Dtos;
using WebHotel.Models;
using WebHotel.Tests.Helpers;
using Xunit;

namespace WebHotel.Tests.Api
{
    public class PaymentsApiTests
    {
        [Fact]
        public async Task GetByBooking_ReturnsPaymentEntries()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedAll(db);
            db.PaymentEntries.AddRange(
                new PaymentEntry { BookingId = 1, Type = PaymentEntryType.Payment, Method = PaymentMethod.Card, Amount = 100m, Description = "Deposit" },
                new PaymentEntry { BookingId = 1, Type = PaymentEntryType.Charge, Method = PaymentMethod.PosTerminal, Amount = 25m, Description = "Mini bar" }
            );
            db.SaveChanges();
            var controller = new PaymentsApiController(db);

            var result = await controller.GetByBooking(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var entries = Assert.IsAssignableFrom<IEnumerable<PaymentEntryDto>>(ok.Value);
            Assert.Equal(2, entries.Count());
        }

        [Fact]
        public async Task GetByBooking_InvalidBooking_ReturnsNotFound()
        {
            using var db = TestDbHelper.CreateContext();
            var controller = new PaymentsApiController(db);

            var result = await controller.GetByBooking(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Summary_ReturnsCorrectFinancials()
        {
            using var db = TestDbHelper.CreateContext();
            TestDbHelper.SeedAll(db);
            db.PaymentEntries.AddRange(
                new PaymentEntry { BookingId = 1, Type = PaymentEntryType.Payment, Method = PaymentMethod.Card, Amount = 200m, Description = "Deposit" },
                new PaymentEntry { BookingId = 1, Type = PaymentEntryType.Charge, Method = PaymentMethod.ManualAdjustment, Amount = 50m, Description = "Room service" },
                new PaymentEntry { BookingId = 1, Type = PaymentEntryType.Refund, Method = PaymentMethod.BankTransfer, Amount = 30m, Description = "Refund" }
            );
            db.SaveChanges();
            var controller = new PaymentsApiController(db);

            var result = await controller.Summary(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            // RoomTotal=450, Charges=50, Payments=200, Refunds=30
            // BalanceDue = 450 + 50 + 30 - 200 = 330
            dynamic summary = ok.Value!;
            var type = ok.Value!.GetType();
            Assert.Equal(450m, (decimal)type.GetProperty("RoomTotal")!.GetValue(ok.Value)!);
            Assert.Equal(50m, (decimal)type.GetProperty("ExtraCharges")!.GetValue(ok.Value)!);
            Assert.Equal(200m, (decimal)type.GetProperty("PaymentsReceived")!.GetValue(ok.Value)!);
            Assert.Equal(30m, (decimal)type.GetProperty("RefundTotal")!.GetValue(ok.Value)!);
            Assert.Equal(330m, (decimal)type.GetProperty("BalanceDue")!.GetValue(ok.Value)!);
        }

        [Fact]
        public async Task Summary_InvalidBooking_ReturnsNotFound()
        {
            using var db = TestDbHelper.CreateContext();
            var controller = new PaymentsApiController(db);

            var result = await controller.Summary(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
