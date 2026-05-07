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
        public DbSet<SystemEmailTemplate> SystemEmailTemplates { get; set; }
        public DbSet<InsuranceAccount> InsuranceAccounts { get; set; }
        public DbSet<ServicePricingProfile> ServicePricingProfiles { get; set; }
        public DbSet<InsuranceAccountServiceRate> InsuranceAccountServiceRates { get; set; }
        public DbSet<DriverPayroll> DriverPayrolls { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserFcmToken> UserFcmTokens { get; set; }
        public DbSet<VehicleCatalogMake> VehicleCatalogMakes { get; set; }
        public DbSet<VehicleCatalogModel> VehicleCatalogModels { get; set; }
        public DbSet<VehicleCatalogSyncState> VehicleCatalogSyncStates { get; set; }
        public DbSet<PendingDriverSignup> PendingDriverSignups { get; set; }
        public DbSet<AuthOtpRecord> AuthOtpRecords { get; set; }
        public DbSet<TruckType> TruckTypes { get; set; }
        public DbSet<Truck> Trucks { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }
        public DbSet<InvoiceImage> InvoiceImages { get; set; }
        public DbSet<CompanyEmployee> CompanyEmployees { get; set; }
        public DbSet<WorkspaceTeam> WorkspaceTeams { get; set; }
        public DbSet<WorkspaceTeamMember> WorkspaceTeamMembers { get; set; }
        public DbSet<TaskBoard> TaskBoards { get; set; }
        public DbSet<TaskBoardMember> TaskBoardMembers { get; set; }
        public DbSet<TaskBoardColumn> TaskBoardColumns { get; set; }
        public DbSet<TaskTicket> TaskTickets { get; set; }

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

            builder.Entity<Job>()
                .HasIndex(j => new { j.Status, j.CreatedAt })
                .HasDatabaseName("IX_Jobs_Status_CreatedAt");

            builder.Entity<Job>()
                .HasIndex(j => new { j.DriverId, j.Status })
                .HasDatabaseName("IX_Jobs_DriverId_Status");
            
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

            builder.Entity<SystemEmailTemplate>()
                .HasIndex(t => t.EventKey)
                .IsUnique();
            builder.Entity<SystemEmailTemplate>()
                .Property(t => t.EventKey)
                .HasMaxLength(64)
                .IsRequired();
            builder.Entity<SystemEmailTemplate>()
                .Property(t => t.Subject)
                .HasMaxLength(500)
                .IsRequired();
            builder.Entity<SystemEmailTemplate>()
                .Property(t => t.HtmlBody)
                .IsRequired();

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

            builder.Entity<InsuranceAccountServiceRate>()
                .HasIndex(x => new { x.InsuranceAccountId, x.ServicePricingProfileId })
                .IsUnique();

            builder.Entity<InsuranceAccountServiceRate>()
                .HasOne(x => x.InsuranceAccount)
                .WithMany()
                .HasForeignKey(x => x.InsuranceAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InsuranceAccountServiceRate>()
                .HasOne(x => x.ServicePricingProfile)
                .WithMany()
                .HasForeignKey(x => x.ServicePricingProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
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

            builder.Entity<PendingDriverSignup>(e =>
            {
                e.HasKey(x => x.NormalizedEmail);
                e.Property(x => x.NormalizedEmail).HasMaxLength(256);
                e.Property(x => x.Email).HasMaxLength(256).IsRequired();
                e.Property(x => x.ProtectedPassword).IsRequired();
                e.Property(x => x.FullName).HasMaxLength(256);
                e.Property(x => x.PhoneNumber).HasMaxLength(32);
                e.Property(x => x.RoleId).HasMaxLength(450).IsRequired();
            });

            builder.Entity<AuthOtpRecord>(e =>
            {
                e.HasIndex(x => new { x.NormalizedEmail, x.Purpose, x.Used });
                e.Property(x => x.NormalizedEmail).HasMaxLength(256);
                e.Property(x => x.Email).HasMaxLength(256);
                e.Property(x => x.OtpHash).HasMaxLength(512);
            });

            builder.Entity<TruckType>()
                .HasIndex(t => t.Name)
                .IsUnique();

            builder.Entity<Truck>()
                .HasOne(t => t.TruckType)
                .WithMany(tt => tt.Trucks)
                .HasForeignKey(t => t.TruckTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Truck>()
                .HasIndex(t => new { t.TruckTypeId, t.UnitLabel })
                .IsUnique();

            builder.Entity<Job>()
                .HasOne(j => j.Truck)
                .WithMany(t => t.Jobs)
                .HasForeignKey(j => j.TruckId)
                .OnDelete(DeleteBehavior.SetNull);

            // Invoice relationships
            builder.Entity<Invoice>()
                .HasOne(i => i.CreatedBy)
                .WithMany()
                .HasForeignKey(i => i.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Invoice>()
                .HasOne(i => i.Job)
                .WithMany()
                .HasForeignKey(i => i.JobId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            builder.Entity<Invoice>()
                .HasIndex(i => i.CreatedAt);

            builder.Entity<InvoiceLineItem>()
                .HasOne(li => li.Invoice)
                .WithMany(i => i.LineItems)
                .HasForeignKey(li => li.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InvoiceImage>()
                .HasOne(img => img.Invoice)
                .WithMany(i => i.Images)
                .HasForeignKey(img => img.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InvoiceImage>()
                .HasIndex(img => new { img.InvoiceId, img.SortOrder });

            builder.Entity<CompanyEmployee>()
                .HasIndex(e => e.UserId)
                .IsUnique();

            builder.Entity<CompanyEmployee>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WorkspaceTeam>()
                .HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<WorkspaceTeamMember>()
                .HasKey(m => new { m.TeamId, m.UserId });

            builder.Entity<WorkspaceTeamMember>()
                .HasOne(m => m.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(m => m.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WorkspaceTeamMember>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskBoard>()
                .HasOne(b => b.Team)
                .WithMany(t => t.Boards)
                .HasForeignKey(b => b.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskBoard>()
                .HasOne(b => b.CreatedBy)
                .WithMany()
                .HasForeignKey(b => b.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<TaskBoardMember>()
                .HasKey(m => new { m.BoardId, m.UserId });

            builder.Entity<TaskBoardMember>()
                .HasOne(m => m.Board)
                .WithMany(b => b.Members)
                .HasForeignKey(m => m.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskBoardMember>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskBoardColumn>()
                .HasOne(c => c.Board)
                .WithMany(b => b.Columns)
                .HasForeignKey(c => c.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskBoardColumn>()
                .HasIndex(c => new { c.BoardId, c.SortOrder });

            builder.Entity<TaskTicket>()
                .HasOne(t => t.Board)
                .WithMany(b => b.Tickets)
                .HasForeignKey(t => t.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskTicket>()
                .HasOne(t => t.Column)
                .WithMany(c => c.Tickets)
                .HasForeignKey(t => t.ColumnId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TaskTicket>()
                .HasOne(t => t.Assignee)
                .WithMany()
                .HasForeignKey(t => t.AssigneeUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<TaskTicket>()
                .HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
        
    }
}