using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Models
{
    public class DriverViewModel
    {
        public int DriverId { get; set; }
        [Required(ErrorMessage = "Name is required.")]

        public string Name { get; set; }
        [Required(ErrorMessage = "Vehicle type is required.")]

        public string VehicleType { get; set; }

        public string Status { get; set; }
        public bool  isAvailable{ get; set; }

        public string ProfilePicture { get; set; } // مسار صورة الملف الشخصي
        public string LicensePicture { get; set; } // مسار صورة رخصة القيادة
        public string workPermitPicture { get; set; } // مسار صورة رخصة القيادة
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]

        public string phone { get; set; }

        public string Password { get; set; } // لتعديل كلمة المرور
      
        public string TrackingDriver { get; set; }

        [Required(ErrorMessage = "Current location is required.")]

        public string CurrentLocation { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsAvailable { get; set; }  // السائق متاح للعمل أم لا
        public bool? IsApproved { get; set; }   // حالة الموافقة
        public bool IsNewDriver { get; set; }  // لتمييز السائقين الجدد

    }
}
