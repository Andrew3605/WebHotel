using System.ComponentModel.DataAnnotations;

namespace WebHotel.ViewModels
{
    public class PaymentVm
    {
        // Booking summary (read-only in the form)
        public int BookingId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal TotalPrice { get; set; }

        // Payment inputs (fake gateway)
        [Required, Display(Name = "Name on card")]
        public string NameOnCard { get; set; } = string.Empty;

        [Required, Display(Name = "Card number")]
        [RegularExpression(@"^\d{12,19}$", ErrorMessage = "Enter 12–19 digits.")]
        public string CardNumber { get; set; } = string.Empty;

        [Required, Range(1, 12)]
        [Display(Name = "Exp. month")]
        public int ExpMonth { get; set; }

        [Required, Range(2000, 2100)]
        [Display(Name = "Exp. year")]
        public int ExpYear { get; set; }

        [Required, Display(Name = "CVV")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3–4 digits.")]
        public string Cvv { get; set; } = string.Empty;
    }
}
