using System.ComponentModel.DataAnnotations;

namespace WebHotel.ViewModels
{
    public enum BookingPaymentOption
    {
        Deposit = 1,
        OutstandingBalance = 2
    }

    public class PaymentVm
    {
        public int BookingId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal RoomTotal { get; set; }
        public decimal ExtraCharges { get; set; }
        public decimal PaidToDate { get; set; }
        public decimal BalanceDue { get; set; }
        public decimal DepositAmount { get; set; }
        public bool HasPreviousPayments { get; set; }

        [Required]
        [Display(Name = "Payment option")]
        public BookingPaymentOption PaymentOption { get; set; } = BookingPaymentOption.Deposit;

        [Required, Display(Name = "Name on card")]
        public string NameOnCard { get; set; } = string.Empty;

        [Required, Display(Name = "Card number")]
        [RegularExpression(@"^\d{12,19}$", ErrorMessage = "Enter 12-19 digits.")]
        public string CardNumber { get; set; } = string.Empty;

        [Required, Range(1, 12)]
        [Display(Name = "Exp. month")]
        public int ExpMonth { get; set; }

        [Required, Range(2000, 2100)]
        [Display(Name = "Exp. year")]
        public int ExpYear { get; set; }

        [Required, Display(Name = "CVV")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3-4 digits.")]
        public string Cvv { get; set; } = string.Empty;
    }
}
