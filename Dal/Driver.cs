using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal
{
    public class Driver : IdentityUser<int>
    {
        public string ProfilePicture { get; set; } // مسار صورة الملف الشخصي
        public string LicensePicture { get; set; } // مسار صورة رخصة القيادة
        public string workPermitPicture { get; set; }

        public bool IsDriver { get; set; } // تحديد إذا كان هذا المستخدم هو سائق معتمد بعد الموافقة


        public string Status { get; set; }  // مثل (متاح، لاستلام طلب اخر، غير متاح)
        public string CurrentLocation { get; set; }

        [Required]
        public string VehicleType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool RememberMe { get; set; }
        public string TrackingDriver { get; set; }

        public bool IsAvailable { get; set; }  // السائق متاح للعمل أم لا
        public bool? IsApproved { get; set; }   // حالة الموافقة
        public bool IsNewDriver { get; set; }  // لتمييز السائقين الجدد
        public string OTP { get; set; }
        public string PhoneNumberWithCountryCode { get; set; }  // رقم الهاتف مع رمز الدولة

        public DateTime OTPGeneratedAt { get; set; } // حفظ وقت إنشاء OTP


        public int OTPAttempts { get; set; }  // عدد المحاولات

        public List<Customer_driver> Customer_drivers { get; set; } = new List<Customer_driver>();
        public List<Order_Driver> Order_Drivers { get; set; } = new List<Order_Driver>();



    }
}
