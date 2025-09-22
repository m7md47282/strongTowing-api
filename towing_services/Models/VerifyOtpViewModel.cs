using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Models
{
    public class VerifyOtpViewModel
    {

        [Required(ErrorMessage = "OTP is required")]
        public string OTP { get; set; }

        public string Email { get; set; }
        public int OTPAttempts { get; set; }

        public IFormFile ProfilePicture { get; set; }
        public IFormFile LicensePicture { get; set; }

    }
}
