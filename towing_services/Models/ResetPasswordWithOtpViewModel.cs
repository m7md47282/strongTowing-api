using System.ComponentModel.DataAnnotations;

namespace towing_services.Models
{
    public class ResetPasswordWithOtpViewModel
    {
        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required, DataType(DataType.Password), Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}
