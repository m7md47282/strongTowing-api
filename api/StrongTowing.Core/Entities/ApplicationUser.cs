using Microsoft.AspNetCore.Identity;

namespace StrongTowing.Core.Entities
{
    // We extend IdentityUser to get built-in username, email, password hashing
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Role Foreign Key - Each user has exactly one role
        public string RoleId { get; set; } = string.Empty;
        public IdentityRole? Role { get; set; }

        // Navigation Property: One driver -> Many Jobs
        public ICollection<Job> AssignedJobs { get; set; } = new List<Job>();
    }
}

