using Microsoft.EntityFrameworkCore;
using StrongTowing.API.Services;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Core.Entities;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.Tests;

public class PricingCalculatorServiceTests
{
    [Fact]
    public async Task CalculateAsync_UsesAccountProfileAndReturnsExpectedTotal()
    {
        await using var context = CreateContext(nameof(CalculateAsync_UsesAccountProfileAndReturnsExpectedTotal));
        context.SystemSettings.Add(new SystemSettings
        {
            MaxDiscountPercent = 100m,
            DefaultPricingHookupFee = 75m,
            DefaultPricingServiceChargePercent = 0m,
            DefaultPricingTaxPercent = 10m
        });
        context.InsuranceAccounts.Add(new InsuranceAccount
        {
            Name = "Insurance A",
            HookupFee = 60m,
            RateAB = 5m,
            RateBC = 10m,
            RateCA = 3m,
            ServiceChargePercent = 5m,
            TaxPercent = 8m
        });
        await context.SaveChangesAsync();

        var service = new PricingCalculatorService(context);
        var result = await service.CalculateAsync(new PricingQuoteRequestDto
        {
            AccountName = "Insurance A",
            MilesAB = 2m,
            MilesBC = 3m,
            MilesCA = 4m,
            ExtraItemsTotal = 20m,
            DiscountAmount = 10m
        });

        Assert.Equal(2m, result.MilesAB);
        Assert.Equal(10m, result.ChargeAB);
        Assert.Equal(30m, result.ChargeBC);
        Assert.Equal(12m, result.ChargeCA);
        Assert.Equal(132m, result.BaseSubtotal);
        Assert.Equal(10m, result.DiscountAmount);
        Assert.Equal(122m, result.AfterDiscount);
        Assert.Equal(6.10m, result.ServiceChargeAmount);
        Assert.Equal(10.25m, result.TaxAmount);
        Assert.Equal(138.35m, result.GrandTotal);
    }

    [Fact]
    public async Task CalculateAsync_ClampsDiscountToPolicyMaxPercent()
    {
        await using var context = CreateContext(nameof(CalculateAsync_ClampsDiscountToPolicyMaxPercent));
        context.SystemSettings.Add(new SystemSettings
        {
            MaxDiscountPercent = 20m,
            DefaultPricingHookupFee = 0m,
            DefaultPricingServiceChargePercent = 0m,
            DefaultPricingTaxPercent = 0m
        });
        await context.SaveChangesAsync();

        var service = new PricingCalculatorService(context);
        var result = await service.CalculateAsync(new PricingQuoteRequestDto
        {
            MilesAB = 10m,
            RateAB = 10m,
            DiscountAmount = 80m
        });

        Assert.Equal(100m, result.BaseSubtotal);
        Assert.Equal(20m, result.DiscountAmount);
        Assert.Equal(80m, result.GrandTotal);
    }

    [Fact]
    public async Task CalculateAsync_RequiresReasonWhenManualOverridePolicyEnabled()
    {
        await using var context = CreateContext(nameof(CalculateAsync_RequiresReasonWhenManualOverridePolicyEnabled));
        context.SystemSettings.Add(new SystemSettings
        {
            AllowManualTotalOverride = true,
            ManualOverrideRequiresReason = true
        });
        await context.SaveChangesAsync();

        var service = new PricingCalculatorService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CalculateAsync(new PricingQuoteRequestDto
        {
            MilesAB = 1m,
            RateAB = 10m,
            ManualTotalOverride = 1m
        }));

        Assert.Contains("reason is required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ApplicationDbContext(options);
    }
}
