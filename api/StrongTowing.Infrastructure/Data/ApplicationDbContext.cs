using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Core.Entities;

namespace StrongTowing.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobPhoto> JobPhotos { get; set; } // Added this one

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Enforce unique VIN
            builder.Entity<Vehicle>().HasIndex(v => v.VIN).IsUnique();
            
            // Configure RoleId foreign key relationship
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
            
            // Make RoleId required (non-nullable)
            builder.Entity<ApplicationUser>()
                .Property(u => u.RoleId)
                .IsRequired();

            // Vehicle -> Owner (client) FK - no cascade to avoid multiple cascade paths
            builder.Entity<Vehicle>()
                .HasOne(v => v.Owner)
                .WithMany()
                .HasForeignKey(v => v.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Job <-> ApplicationUser relationships
            // NOTE: Job has TWO navigations to ApplicationUser (Driver + StatusUpdatedBy),
            // so we must explicitly map them to avoid EF ambiguity at design time.
            builder.Entity<Job>()
                .HasOne(j => j.Driver)
                .WithMany(u => u.AssignedJobs)
                .HasForeignKey(j => j.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Job>()
                .HasOne(j => j.StatusUpdatedBy)
                .WithMany()
                .HasForeignKey(j => j.StatusUpdatedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
        
    }
}