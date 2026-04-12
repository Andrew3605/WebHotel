using System.ComponentModel.DataAnnotations;
using WebHotel.Models;

namespace WebHotel.ViewModels
{
    public class FrontDeskEntryVm
    {
        public int BookingId { get; set; }

        [Required]
        [Display(Name = "Entry type")]
        public PaymentEntryType Type { get; set; } = PaymentEntryType.Charge;

        [Required]
        [Display(Name = "Method")]
        public PaymentMethod Method { get; set; } = PaymentMethod.PosTerminal;

        [Required]
        [StringLength(120)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(typeof(decimal), "0.01", "999999")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string? Reference { get; set; }
    }
}
