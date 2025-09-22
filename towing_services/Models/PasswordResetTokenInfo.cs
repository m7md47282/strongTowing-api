using System;

namespace towing_services.Models
{
    public class PasswordResetTokenInfo
    {

      
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
    }
}
