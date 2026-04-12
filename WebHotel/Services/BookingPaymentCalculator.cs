using WebHotel.Models;

namespace WebHotel.Services
{
    public static class BookingPaymentCalculator
    {
        public const decimal DefaultDepositRate = 0.20m;

        public static decimal GetExtraCharges(IEnumerable<PaymentEntry>? entries)
        {
            return (entries ?? Enumerable.Empty<PaymentEntry>())
                .Where(x => x.Type == PaymentEntryType.Charge)
                .Sum(x => x.Amount);
        }

        public static decimal GetPaymentsReceived(IEnumerable<PaymentEntry>? entries)
        {
            return (entries ?? Enumerable.Empty<PaymentEntry>())
                .Where(x => x.Type == PaymentEntryType.Payment)
                .Sum(x => x.Amount);
        }

        public static decimal GetRefundTotal(IEnumerable<PaymentEntry>? entries)
        {
            return (entries ?? Enumerable.Empty<PaymentEntry>())
                .Where(x => x.Type == PaymentEntryType.Refund)
                .Sum(x => x.Amount);
        }

        public static decimal GetBalanceDue(decimal roomTotal, IEnumerable<PaymentEntry>? entries)
        {
            return roomTotal + GetExtraCharges(entries) + GetRefundTotal(entries) - GetPaymentsReceived(entries);
        }

        public static decimal GetDepositAmount(decimal roomTotal, IEnumerable<PaymentEntry>? entries, decimal depositRate = DefaultDepositRate)
        {
            var balanceDue = GetBalanceDue(roomTotal, entries);
            if (balanceDue <= 0)
                return 0m;

            var deposit = Math.Round(roomTotal * depositRate, 2);
            return Math.Min(deposit, balanceDue);
        }

        public static decimal GetRefundableAmount(IEnumerable<PaymentEntry>? entries)
        {
            return GetPaymentsReceived(entries) - GetRefundTotal(entries);
        }

        public static bool IsFullyPaid(decimal roomTotal, IEnumerable<PaymentEntry>? entries)
        {
            return GetBalanceDue(roomTotal, entries) <= 0.01m;
        }
    }
}
