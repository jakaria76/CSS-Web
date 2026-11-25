using System.ComponentModel.DataAnnotations;

namespace CSS.ViewModels
{
    public class OtpVerifyViewModel
    {
        [Required]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Please enter the 6-digit OTP code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must contain 6 digits.")]
        [Display(Name = "OTP Code")]
        public string? OtpCode { get; set; }
    }
}
