using System.ComponentModel.DataAnnotations;
using Dal;

namespace towing_services.Models
{
    public class CreateHRViewModel
    {
        public int Id { get; set; } // ضروري للتحديث

 

        [Required, EmailAddress]
        public string Email { get; set; }


        [Required]
        public string PhoneNumber { get; set; }

        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string FullName { get; set; }

    }
}
