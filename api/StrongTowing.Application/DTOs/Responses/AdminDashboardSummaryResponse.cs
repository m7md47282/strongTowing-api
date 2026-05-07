namespace StrongTowing.Application.DTOs.Responses;

public class ServiceTypeBreakdownDto
{
    public int Towing { get; set; }
    public int Roadside { get; set; }
    public int JumpStart { get; set; }
    public int TireChange { get; set; }
    public int Other { get; set; }
    public int Total { get; set; }
}

public class DailyJobCountDto
{
    /// <summary>Short label for charts (e.g. &quot;Apr 28&quot;).</summary>
    public string DateLabel { get; set; } = string.Empty;

    /// <summary>ISO calendar date yyyy-MM-dd.</summary>
    public string DateKey { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class AdminDashboardSummaryResponse
{
    /// <summary>Jobs where status is not Completed (matches legacy dashboard semantics).</summary>
    public int ActiveRequests { get; set; }

    public int CompletedToday { get; set; }

    public List<JobDto> RecentActiveJobs { get; set; } = new();

    public ServiceTypeBreakdownDto ServiceBreakdown { get; set; } = new();

    /// <summary>Seven buckets aligned with the admin dashboard chart (client sends window).</summary>
    public List<DailyJobCountDto> JobsCreatedLast7Days { get; set; } = new();
}
