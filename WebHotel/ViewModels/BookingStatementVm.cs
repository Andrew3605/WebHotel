using WebHotel.Models;

namespace WebHotel.ViewModels
{
    public class BookingStatementVm
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal RoomTotal { get; set; }
        public decimal ExtraCharges { get; set; }
        public decimal PaymentsReceived { get; set; }
        public decimal RefundTotal { get; set; }
        public decimal BalanceDue { get; set; }
        public bool IsPaid { get; set; }
        public bool CanManageFrontDesk { get; set; }
        public IReadOnlyList<PaymentEntry> Entries { get; set; } = Array.Empty<PaymentEntry>();
        public FrontDeskEntryVm FrontDeskEntry { get; set; } = new();
    }
}
