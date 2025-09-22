using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Models
{
    public class OtpViewModel
    {
        public double RemainingOtpValidity { get; set; }
        public int RemainingOtpRequests { get; set; }
        public int RemainingCooldownMinutes { get; set; }
        public int RemainingCooldownSeconds { get; set; }
    }

}
