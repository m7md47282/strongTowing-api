using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Models
{
    public class RegisterDriverViewModel
    {
        
            [Required]
        public string Name { get; set; } // اسم المستخدم الذي سيتم تخزينه في Identity

            [Required]
            [Phone]
            public string Phone { get; set; }

        [Required]
            public string VehicleType { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
    

        [DataType(DataType.Upload)]
        [Required(ErrorMessage = "Profile picture is required.")]

        public IFormFile ProfilePicture { get; set; } // صورة الملف الشخصي

        [DataType(DataType.Upload)]
        [Required(ErrorMessage = "License picture is required.")]

        public IFormFile LicensePicture { get; set; } // صورة رخصة القيادة


        [DataType(DataType.Upload)]
        [Required(ErrorMessage = "Work permit picture is required.")]
        public IFormFile WorkPermitPicture { get; set; } // صورة رخصة العمل


        public string Role { get; set; } // "Driver" أو "Admin"

    }

}



