using System.ComponentModel.DataAnnotations;

namespace towing_services.Models
{
    public class UpdateHRViewModel
    {
        public int Id { get; set; }



        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

    }

}
