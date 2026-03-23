namespace StrongTowing.Application.DTOs.Responses
{
    public class FinancialSummaryResponse
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalRefunded { get; set; }
        public decimal NetRevenue { get; set; }
        public int TotalPayments { get; set; }
        public int TotalJobs { get; set; }
        public decimal AverageJobCost { get; set; }
        public decimal CancellationFeeRevenue { get; set; }
        public FinancialRevenueByMethodResponse RevenueByMethod { get; set; } = new();
        public FinancialStatusCountsResponse StatusCounts { get; set; } = new();
        public FinancialReportPeriodResponse Period { get; set; } = new();
    }

    public class FinancialRevenueByMethodResponse
    {
        public decimal Card { get; set; }
        public decimal PaymentLink { get; set; }
        public decimal Cash { get; set; }
    }

    public class FinancialStatusCountsResponse
    {
        public int Pending { get; set; }
        public int Paid { get; set; }
        public int Failed { get; set; }
        public int Refunded { get; set; }
        public int PartiallyRefunded { get; set; }
        public int UnderReview { get; set; }
        public int Authorized { get; set; }
        public int Cancelled { get; set; }
    }

    public class FinancialReportPeriodResponse
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
