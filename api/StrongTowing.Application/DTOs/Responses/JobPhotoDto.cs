namespace StrongTowing.Application.DTOs.Responses;

public class JobPhotoDto
{
    public int Id { get; set; }
    /// <summary>Public path under the API host (e.g. /uploads/job-photos/1/abc.jpg).</summary>
    public string Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}
