namespace StrongTowing.Application.DTOs.Requests
{
    public class UpdateSystemSettingsRequest
    {
        public decimal DriverCommissionPercentage { get; set; }
        public string? StripePublicKey { get; set; }
        public bool StripeEnabled { get; set; }
    }
}
