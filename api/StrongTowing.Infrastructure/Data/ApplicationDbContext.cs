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
        public DbSet<Payment> Payments { get; set; }
        public DbSet<CashCollection> CashCollections { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }
        public DbSet<InsuranceAccount> InsuranceAccounts { get; set; }
        public DbSet<ServicePricingProfile> ServicePricingProfiles { get; set; }
        public DbSet<DriverPayroll> DriverPayrolls { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserFcmToken> UserFcmTokens { get; set; }
        public DbSet<VehicleCatalogMake> VehicleCatalogMakes { get; set; }
        public DbSet<VehicleCatalogModel> VehicleCatalogModels { get; set; }
        public DbSet<VehicleCatalogSyncState> VehicleCatalogSyncStates { get; set; }

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
            
            // Payment relationships
            builder.Entity<Payment>()
                .HasOne(p => p.Job)
                .WithMany()
                .HasForeignKey(p => p.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasIndex(p => new { p.PaymentStatus, p.FraudStatus });
            
            builder.Entity<Job>()
                .HasOne(j => j.Payment)
                .WithMany()
                .HasForeignKey(j => j.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // CashCollection relationships
            builder.Entity<CashCollection>()
                .HasOne(cc => cc.Job)
                .WithMany()
                .HasForeignKey(cc => cc.JobId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.Entity<CashCollection>()
                .HasOne(cc => cc.Payment)
                .WithMany()
                .HasForeignKey(cc => cc.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.Entity<CashCollection>()
                .HasOne(cc => cc.Driver)
                .WithMany()
                .HasForeignKey(cc => cc.DriverId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // DriverPayroll relationships
            builder.Entity<DriverPayroll>()
                .HasOne(dp => dp.Driver)
                .WithMany()
                .HasForeignKey(dp => dp.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DriverPayroll>()
                .HasIndex(dp => new { dp.DriverId, dp.PayPeriodStart, dp.PayPeriodEnd })
                .IsUnique();
            
            // SystemSettings - single row table
            builder.Entity<SystemSettings>()
                .HasIndex(s => s.Id)
                .IsUnique();

            builder.Entity<InsuranceAccount>()
                .HasIndex(a => a.Name)
                .IsUnique();

            builder.Entity<InsuranceAccount>()
                .Property(a => a.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Entity<ServicePricingProfile>()
                .HasIndex(s => s.Name)
                .IsUnique();

            builder.Entity<ServicePricingProfile>()
                .Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            // RefreshToken configuration
            builder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            builder.Entity<RefreshToken>()
                .HasIndex(rt => rt.UserId);

            builder.Entity<UserFcmToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            builder.Entity<UserFcmToken>()
                .HasOne(t => t.User)
                .WithMany(u => u.FcmTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VehicleCatalogMake>()
                .HasIndex(m => m.NhtsaMakeId)
                .IsUnique();

            builder.Entity<VehicleCatalogMake>()
                .Property(m => m.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Entity<VehicleCatalogModel>()
                .HasOne(m => m.Make)
                .WithMany(mk => mk.Models)
                .HasForeignKey(m => m.MakeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VehicleCatalogModel>()
                .HasIndex(m => new { m.MakeId, m.NhtsaModelId })
                .IsUnique();

            builder.Entity<VehicleCatalogModel>()
                .Property(m => m.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Entity<VehicleCatalogSyncState>()
                .HasKey(s => s.Id);

            builder.Entity<VehicleCatalogSyncState>()
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();
        }
        
    }
}