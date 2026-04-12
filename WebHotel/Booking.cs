using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using WebHotel.Services;

namespace WebHotel.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckIn { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckOut { get; set; }

        [Precision(18, 2)]
        public decimal TotalPrice { get; set; }

        public bool IsPaid { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PaymentEntry> PaymentEntries { get; set; } = new List<PaymentEntry>();

        [NotMapped]
        public decimal ExtraCharges => BookingPaymentCalculator.GetExtraCharges(PaymentEntries);

        [NotMapped]
        public decimal PaymentsReceived => BookingPaymentCalculator.GetPaymentsReceived(PaymentEntries);

        [NotMapped]
        public decimal RefundTotal => BookingPaymentCalculator.GetRefundTotal(PaymentEntries);

        [NotMapped]
        public decimal BalanceDue => BookingPaymentCalculator.GetBalanceDue(TotalPrice, PaymentEntries);
    }
}
