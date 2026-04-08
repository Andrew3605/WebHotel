using System.ComponentModel.DataAnnotations;

namespace WebHotel.ViewModels
{
    public class AdminResetCustomerPasswordVm
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public bool HasLinkedAccount { get; set; }

        public string? LinkedLoginEmail { get; set; }

        [Display(Name = "New Password")]
        [Required, DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Display(Name = "Confirm Password")]
        [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
