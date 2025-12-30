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
        }
    }
}