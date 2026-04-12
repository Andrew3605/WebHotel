using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WebHotel.Models
{
    public enum PaymentEntryType
    {
        Payment = 1,
        Charge = 2,
        Refund = 3
    }

    public enum PaymentMethod
    {
        Card = 1,
        Cash = 2,
        PosTerminal = 3,
        BankTransfer = 4,
        ManualAdjustment = 5
    }

    public class PaymentEntry
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Required]
        public PaymentEntryType Type { get; set; } = PaymentEntryType.Payment;

        [Required]
        public PaymentMethod Method { get; set; } = PaymentMethod.Card;

        [Required]
        [StringLength(120)]
        public string Description { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string? Reference { get; set; }

        [StringLength(30)]
        public string? MaskedCardNumber { get; set; }

        [StringLength(120)]
        public string? ProcessedBy { get; set; }

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
