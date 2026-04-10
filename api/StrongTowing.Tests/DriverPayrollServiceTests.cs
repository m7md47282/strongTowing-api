using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StrongTowing.API.Services;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;
using StrongTowing.Tests.Fakes;

namespace StrongTowing.Tests;

public class DriverPayrollServiceTests
{
    [Fact]
    public async Task GenerateOrRefreshAsync_WithNoJobs_ReturnsEmptyRows()
    {
        await using var ctx = CreateContext(nameof(GenerateOrRefreshAsync_WithNoJobs_ReturnsEmptyRows));
        ctx.SystemSettings.Add(new SystemSettings { DriverCommissionPercentage = 30m });
        await ctx.SaveChangesAsync();

        var svc = new DriverPayrollService(ctx, new NoOpSmsNotificationService());
        var result = await svc.GenerateOrRefreshAsync(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 14, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(0, result.RowsUpserted);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public async Task FinalizeAsync_FromDraft_SetsFinalized()
    {
        await using var ctx = CreateContext(nameof(FinalizeAsync_FromDraft_SetsFinalized));
        SeedRoles(ctx);
        ctx.Users.Add(CreateUser("d1", "Driver One"));
        ctx.SystemSettings.Add(new SystemSettings { DriverCommissionPercentage = 30m });
        var ps = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var pe = new DateTime(2026, 1, 14, 0, 0, 0, DateTimeKind.Utc);
        ctx.DriverPayrolls.Add(new DriverPayroll
        {
            DriverId = "d1",
            PayPeriodStart = ps,
            PayPeriodEnd = pe,
            TotalJobs = 0,
            TotalJobRevenue = 0,
            TotalJobMinutes = 0,
            CommissionPercentage = 30m,
            GrossEarnings = 0,
            CashCollections = 0,
            NetPay = 0,
            Status = DriverPayrollStatuses.Draft
        });
        await ctx.SaveChangesAsync();

        var svc = new DriverPayrollService(ctx, new NoOpSmsNotificationService());
        var dto = await svc.FinalizeAsync(1, "admin-1", CancellationToken.None);

        Assert.Equal(DriverPayrollStatuses.Finalized, dto.Status);
        Assert.NotNull(dto.FinalizedAt);
        Assert.Equal("admin-1", dto.FinalizedBy);
    }

    [Fact]
    public async Task GenerateOrRefreshAsync_WithCompletedJob_ComputesCommissionAndNet()
    {
        await using var ctx = CreateContext(nameof(GenerateOrRefreshAsync_WithCompletedJob_ComputesCommissionAndNet));
        SeedRoles(ctx);
        ctx.Users.Add(CreateUser("owner1", "Owner"));
        ctx.Users.Add(CreateUser("d1", "Driver One"));
        ctx.SystemSettings.Add(new SystemSettings { DriverCommissionPercentage = 25m });
        ctx.Vehicles.Add(new Vehicle
        {
            VIN = "TESTVIN12345678",
            OwnerId = "owner1"
        });
        await ctx.SaveChangesAsync();

        var completed = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        ctx.Jobs.Add(new Job
        {
            VehicleId = 1,
            DriverId = "d1",
            Status = JobStatus.Completed,
            Cost = 400m,
            CompletedAt = completed,
            CreatedAt = completed.AddMinutes(-90)
        });
        await ctx.SaveChangesAsync();

        var svc = new DriverPayrollService(ctx, new NoOpSmsNotificationService());
        var result = await svc.GenerateOrRefreshAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1, result.RowsUpserted);
        Assert.Single(result.Rows);
        var row = result.Rows[0];
        Assert.Equal(400m, row.TotalJobRevenue);
        Assert.Equal(25m, row.CommissionPercentage);
        Assert.Equal(100m, row.GrossEarnings);
        Assert.Equal(0m, row.CashCollections);
        Assert.Equal(100m, row.NetPay);
        Assert.Equal(90, row.TotalJobMinutes);
        Assert.Equal(DriverPayrollStatuses.Draft, row.Status);
    }

    [Fact]
    public async Task GenerateOrRefreshAsync_CashToDriverPayroll_ReducesNetPay()
    {
        await using var ctx = CreateContext(nameof(GenerateOrRefreshAsync_CashToDriverPayroll_ReducesNetPay));
        SeedRoles(ctx);
        ctx.Users.Add(CreateUser("owner1", "Owner"));
        ctx.Users.Add(CreateUser("d1", "Driver One"));
        ctx.SystemSettings.Add(new SystemSettings { DriverCommissionPercentage = 20m });
        ctx.Vehicles.Add(new Vehicle { VIN = "CASHVIN12345678", OwnerId = "owner1" });
        await ctx.SaveChangesAsync();

        var completed = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        ctx.Jobs.Add(new Job
        {
            VehicleId = 1,
            DriverId = "d1",
            Status = JobStatus.Completed,
            Cost = 500m,
            CompletedAt = completed,
            CreatedAt = completed.AddMinutes(-30),
            BillingPaymentMode = JobBillingModes.CashToDriverPayroll,
            DriverCashCollectedAmount = 80m,
            PayrollDeductionAmount = 80m
        });
        await ctx.SaveChangesAsync();

        var svc = new DriverPayrollService(ctx, new NoOpSmsNotificationService());
        var result = await svc.GenerateOrRefreshAsync(
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));

        var row = Assert.Single(result.Rows);
        Assert.Equal(100m, row.GrossEarnings);
        Assert.Equal(80m, row.CashCollections);
        Assert.Equal(20m, row.NetPay);
    }

    private static void SeedRoles(ApplicationDbContext ctx)
    {
        ctx.Roles.Add(new IdentityRole { Id = "3", Name = UserRoles.Driver, NormalizedName = UserRoles.Driver.ToUpperInvariant() });
        ctx.Roles.Add(new IdentityRole { Id = "4", Name = UserRoles.User, NormalizedName = UserRoles.User.ToUpperInvariant() });
    }

    private static ApplicationUser CreateUser(string id, string fullName) =>
        new()
        {
            Id = id,
            UserName = $"{id}@test.com",
            NormalizedUserName = $"{id.ToUpperInvariant()}@TEST.COM",
            Email = $"{id}@test.com",
            NormalizedEmail = $"{id.ToUpperInvariant()}@TEST.COM",
            EmailConfirmed = true,
            RoleId = id.StartsWith("d", StringComparison.Ordinal) ? "3" : "4",
            FullName = fullName,
            PasswordHash = "x",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ApplicationDbContext(options);
    }
}
