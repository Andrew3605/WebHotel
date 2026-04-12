using WebHotel.Models;
using WebHotel.Services;
using Xunit;

namespace WebHotel.Tests.Services
{
    public class BookingPaymentCalculatorTests
    {
        [Fact]
        public void GetBalanceDue_WithNoEntries_ReturnsRoomTotal()
        {
            var balance = BookingPaymentCalculator.GetBalanceDue(300m, Array.Empty<PaymentEntry>());

            Assert.Equal(300m, balance);
        }

        [Fact]
        public void GetBalanceDue_IncludesChargesPaymentsAndRefunds()
        {
            var entries = new[]
            {
                CreateEntry(PaymentEntryType.Charge, 45m),
                CreateEntry(PaymentEntryType.Payment, 100m),
                CreateEntry(PaymentEntryType.Refund, 20m)
            };

            var balance = BookingPaymentCalculator.GetBalanceDue(400m, entries);

            Assert.Equal(365m, balance);
        }

        [Fact]
        public void GetDepositAmount_ReturnsTwentyPercentForFirstPayment()
        {
            var deposit = BookingPaymentCalculator.GetDepositAmount(500m, Array.Empty<PaymentEntry>());

            Assert.Equal(100m, deposit);
        }

        [Fact]
        public void GetDepositAmount_DoesNotExceedOutstandingBalance()
        {
            var entries = new[]
            {
                CreateEntry(PaymentEntryType.Payment, 460m)
            };

            var deposit = BookingPaymentCalculator.GetDepositAmount(500m, entries);

            Assert.Equal(40m, deposit);
        }

        [Fact]
        public void GetRefundableAmount_SubtractsRefundsFromPayments()
        {
            var entries = new[]
            {
                CreateEntry(PaymentEntryType.Payment, 250m),
                CreateEntry(PaymentEntryType.Payment, 50m),
                CreateEntry(PaymentEntryType.Refund, 80m)
            };

            var refundable = BookingPaymentCalculator.GetRefundableAmount(entries);

            Assert.Equal(220m, refundable);
        }

        [Fact]
        public void IsFullyPaid_ReturnsTrueWhenBalanceIsEffectivelyZero()
        {
            var entries = new[]
            {
                CreateEntry(PaymentEntryType.Payment, 199.995m)
            };

            var isFullyPaid = BookingPaymentCalculator.IsFullyPaid(200m, entries);

            Assert.True(isFullyPaid);
        }

        private static PaymentEntry CreateEntry(PaymentEntryType type, decimal amount)
        {
            return new PaymentEntry
            {
                Type = type,
                Amount = amount,
                Description = $"{type} entry"
            };
        }
    }
}
