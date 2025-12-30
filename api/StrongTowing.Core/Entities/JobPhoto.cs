namespace StrongTowing.Core.Entities
{
    public class JobPhoto
    {
        public int Id { get; set; }
        public string PhotoUrl { get; set; } = string.Empty; // Path to file on server
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int JobId { get; set; }
        public Job Job { get; set; } = null!;
    }
}


