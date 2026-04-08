namespace StrongTowing.Application.DTOs.Responses
{
    public class PaymentStatisticsDto
    {
        public int TotalPayments { get; set; }
        public decimal TotalRevenue { get; set; }
        public RevenueByMethodDto RevenueByMethod { get; set; } = new();
        public int PendingPayments { get; set; }
        public decimal TotalCashCollected { get; set; }
        public decimal TotalDriverCommissions { get; set; }

        /// <summary>Percentage applied to paid payment amounts for <see cref="TotalDriverCommissions"/> (system settings).</summary>
        public decimal DriverCommissionRatePercent { get; set; }
    }
    
    public class RevenueByMethodDto
    {
        public decimal Card { get; set; }
        public decimal PaymentLink { get; set; }
        public decimal Cash { get; set; }
    }
}
