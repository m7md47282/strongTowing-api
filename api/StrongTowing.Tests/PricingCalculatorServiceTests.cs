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
            RateAB = 5m,
            RateBC = 10m,
            RateCA = 3m
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

        // BC-only mileage: RateBC $/mi from account; AB/CA not billed; hookup line from DefaultPricingHookupFee (75).
        // Tax/service use SystemSettings defaults (10% tax, 0% service charge), not the insurance account.
        Assert.Equal(2m, result.MilesAB);
        Assert.Equal(0m, result.ChargeAB);
        Assert.Equal(30m, result.ChargeBC);
        Assert.Equal(0m, result.ChargeCA);
        Assert.Equal(125m, result.BaseSubtotal);
        Assert.Equal(10m, result.DiscountAmount);
        Assert.Equal(115m, result.AfterDiscount);
        Assert.Equal(0m, result.ServiceChargeAmount);
        Assert.Equal(11.5m, result.TaxAmount);
        Assert.Equal(126.5m, result.GrandTotal);
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
            MilesBC = 10m,
            RateBC = 10m,
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
            MilesBC = 1m,
            RateBC = 10m,
            ManualTotalOverride = 1m
        }));

        Assert.Contains("reason is required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculateAsync_DoesNotDoubleCountCatalogBase_WhenExtraItemsSubsumeIt()
    {
        await using var context = CreateContext(nameof(CalculateAsync_DoesNotDoubleCountCatalogBase_WhenExtraItemsSubsumeIt));
        context.SystemSettings.Add(new SystemSettings
        {
            MaxDiscountPercent = 100m,
            DefaultPricingHookupFee = 0m,
            DefaultPricingServiceChargePercent = 0m,
            DefaultPricingTaxPercent = 0m,
            PricingFreeMiles = 0m
        });
        context.ServicePricingProfiles.Add(new ServicePricingProfile
        {
            Name = "Standard Tow",
            BasePrice = 120m,
            PricePerMile = 6m,
            IsAvailable = true
        });
        await context.SaveChangesAsync();

        var service = new PricingCalculatorService(context);
        var result = await service.CalculateAsync(new PricingQuoteRequestDto
        {
            ServiceName = "Standard Tow",
            MilesBC = 5m,
            RateBC = 6m,
            ExtraItemsTotal = 120m,
            DiscountAmount = 0m
        });

        // UI sends catalog base as a line item (120); must not add profile BasePrice again.
        Assert.Equal(30m, result.ChargeBC);
        Assert.Equal(150m, result.BaseSubtotal);
        Assert.Equal(150m, result.GrandTotal);
    }

    [Fact]
    public async Task CalculateAsync_UsesCatalogBaseOnly_WhenExtraItemsTotalIsZero()
    {
        await using var context = CreateContext(nameof(CalculateAsync_UsesCatalogBaseOnly_WhenExtraItemsTotalIsZero));
        context.SystemSettings.Add(new SystemSettings
        {
            MaxDiscountPercent = 100m,
            DefaultPricingHookupFee = 0m,
            DefaultPricingServiceChargePercent = 0m,
            DefaultPricingTaxPercent = 0m,
            PricingFreeMiles = 0m
        });
        context.ServicePricingProfiles.Add(new ServicePricingProfile
        {
            Name = "Flat Tow",
            BasePrice = 120m,
            PricePerMile = 0m,
            IsAvailable = true
        });
        await context.SaveChangesAsync();

        var service = new PricingCalculatorService(context);
        var result = await service.CalculateAsync(new PricingQuoteRequestDto
        {
            ServiceName = "Flat Tow",
            MilesBC = 0m,
            ExtraItemsTotal = 0m,
            DiscountAmount = 0m
        });

        Assert.Equal(0m, result.ChargeBC);
        Assert.Equal(120m, result.BaseSubtotal);
        Assert.Equal(120m, result.GrandTotal);
    }

    [Fact]
    public async Task CalculateAsync_AddsCatalogBaseAndExtras_WhenExtrasAreLessThanCatalogBase()
    {
        await using var context = CreateContext(nameof(CalculateAsync_AddsCatalogBaseAndExtras_WhenExtrasAreLessThanCatalogBase));
        context.SystemSettings.Add(new SystemSettings
        {
            MaxDiscountPercent = 100m,
            DefaultPricingHookupFee = 0m,
            DefaultPricingServiceChargePercent = 0m,
            DefaultPricingTaxPercent = 0m,
            PricingFreeMiles = 0m
        });
        context.ServicePricingProfiles.Add(new ServicePricingProfile
        {
            Name = "Mixed",
            BasePrice = 120m,
            PricePerMile = 6m,
            IsAvailable = true
        });
        await context.SaveChangesAsync();

        var service = new PricingCalculatorService(context);
        var result = await service.CalculateAsync(new PricingQuoteRequestDto
        {
            ServiceName = "Mixed",
            MilesBC = 0m,
            ExtraItemsTotal = 50m,
            DiscountAmount = 0m
        });

        Assert.Equal(170m, result.BaseSubtotal);
        Assert.Equal(170m, result.GrandTotal);
    }

    /// <summary>
    /// Mirrors dispatcher create-job payload: Tow with "Tow (base)" line (120), loaded 3.19 mi × $6, 10% tax.
    /// Deadhead/enroute quantities in the job JSON are informational; calculator bills BC + line items + hook only.
    /// Expected grand total matches frontend: subtotal 139.14 + tax 13.91 = 153.05.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_MatchesTowJobScenario_FromProductionCurlPayload()
    {
        await using var context = CreateContext(nameof(CalculateAsync_MatchesTowJobScenario_FromProductionCurlPayload));
        context.SystemSettings.Add(new SystemSettings
        {
            MaxDiscountPercent = 100m,
            DefaultPricingHookupFee = 0m,
            DefaultPricingServiceChargePercent = 0m,
            DefaultPricingTaxPercent = 10m,
            PricingFreeMiles = 0m
        });
        context.ServicePricingProfiles.Add(new ServicePricingProfile
        {
            Name = "Tow",
            BasePrice = 120m,
            PricePerMile = 6m,
            IsAvailable = true
        });
        await context.SaveChangesAsync();

        var service = new PricingCalculatorService(context);
        var result = await service.CalculateAsync(new PricingQuoteRequestDto
        {
            ServiceName = "Tow",
            MilesAB = 1.52m,
            MilesBC = 3.19m,
            MilesCA = 3.02m,
            RateBC = 6m,
            ExtraItemsTotal = 120m,
            DiscountAmount = 0m,
            TaxExempt = false,
            TaxPercent = 10m
        });

        Assert.Equal(0m, result.ChargeAB);
        Assert.Equal(0m, result.ChargeCA);
        Assert.Equal(19.14m, result.ChargeBC);
        Assert.Equal(139.14m, result.BaseSubtotal);
        Assert.Equal(13.91m, result.TaxAmount);
        Assert.Equal(153.05m, result.GrandTotal);
    }

    [Fact]
    public async Task CalculateAsync_BillsEnrouteLoadedDeadhead_FromServiceCatalog_WhenSet()
    {
        await using var context = CreateContext(nameof(CalculateAsync_BillsEnrouteLoadedDeadhead_FromServiceCatalog_WhenSet));
        context.SystemSettings.Add(new SystemSettings
        {
            MaxDiscountPercent = 100m,
            DefaultPricingHookupFee = 0m,
            DefaultPricingServiceChargePercent = 0m,
            DefaultPricingTaxPercent = 0m,
            PricingFreeMiles = 0m
        });
        context.ServicePricingProfiles.Add(new ServicePricingProfile
        {
            Name = "Segment Tow",
            BasePrice = 0m,
            PricePerMile = 4m,
            LoadedPricePerMile = 4m,
            EnroutePricePerMile = 2m,
            DeadheadPricePerMile = 1m,
            IsAvailable = true
        });
        await context.SaveChangesAsync();

        var service = new PricingCalculatorService(context);
        var result = await service.CalculateAsync(new PricingQuoteRequestDto
        {
            ServiceName = "Segment Tow",
            MilesAB = 10m,
            MilesBC = 5m,
            MilesCA = 8m,
            ExtraItemsTotal = 0m,
            DiscountAmount = 0m
        });

        Assert.Equal(20m, result.ChargeAB);
        Assert.Equal(20m, result.ChargeBC);
        Assert.Equal(8m, result.ChargeCA);
        Assert.Equal(2m, result.RateAB);
        Assert.Equal(4m, result.RateBC);
        Assert.Equal(1m, result.RateCA);
        Assert.Equal(48m, result.BaseSubtotal);
        Assert.Equal(48m, result.GrandTotal);
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ApplicationDbContext(options);
    }
}
