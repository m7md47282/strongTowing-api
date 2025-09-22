using System;
using System.ComponentModel.DataAnnotations;

namespace towing_services.Models
{
    public class OtpVerificationViewModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string OTPCode { get; set; }
        //
        public DateTime ExpireAtUtc { get; set; }

        public int ResendAttemptsLeft { get; set; }

        public DateTime? ResendLockedUntil { get; set; }

        public bool IsVerifyEnabled { get; set; }

        public bool IsResendEnabled { get; set; }

        public int ResendAttemptsUsed => Math.Max(0, MaxAttempts - ResendAttemptsLeft);

        public bool IsFinalAttempt => ResendAttemptsLeft == 1;

        public bool IsLockedOut => ResendAttemptsLeft == 0;

        public int MaxAttempts { get; set; } = 2;
    }
}
