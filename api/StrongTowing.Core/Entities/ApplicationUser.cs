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
        
        // Password change tracking
        public bool HasChangedPassword { get; set; } = false;
        public DateTime? PasswordChangedAt { get; set; }
        
        // Role Foreign Key - Each user has exactly one role
        public string RoleId { get; set; } = string.Empty;
        public IdentityRole? Role { get; set; }

        /// <summary>When false, dispatch should not assign new jobs (drivers only).</summary>
        public bool IsAvailableForDispatch { get; set; } = true;

        /// <summary>Optional last known GPS from driver app (WGS84).</summary>
        public double? LastKnownLatitude { get; set; }
        public double? LastKnownLongitude { get; set; }
        public DateTime? LastLocationUtc { get; set; }

        // Navigation Property: One driver -> Many Jobs
        public ICollection<Job> AssignedJobs { get; set; } = new List<Job>();

        public ICollection<UserFcmToken> FcmTokens { get; set; } = new List<UserFcmToken>();
    }
}

