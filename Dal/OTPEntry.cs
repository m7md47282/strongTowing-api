using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal
{
    public class OTPEntry
    {
        public int Id { get; set; }
        public string Email { get; set; }  // البريد الإلكتروني الفريد للمستخدم
        public string UserType { get; set; }  // "Admin", "HR", "Driver"
        public string OTPCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpireAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public int AttemptsCount { get; set; } = 0;
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime? ResendLockedUntil { get; set; }
        public int ResendCount { get; set; } = 0;


    }

}
