using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.Abstractions;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Entities;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Services;

public class PricingCalculatorService : IPricingCalculatorService
{
    private readonly ApplicationDbContext _context;

    public PricingCalculatorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PricingQuoteResponseDto> CalculateAsync(
        PricingQuoteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
            ?? new SystemSettings();

        var account = await ResolveAccountAsync(request, cancellationToken);
        var roundingMode = ResolveRoundingMode(settings.PricingRoundingMode);

        var milesAB = EnsureNonNegative(request.MilesAB, nameof(request.MilesAB));
        var milesBC = EnsureNonNegative(request.MilesBC, nameof(request.MilesBC));
        var milesCA = EnsureNonNegative(request.MilesCA, nameof(request.MilesCA));
        var extraItemsTotal = EnsureNonNegative(request.ExtraItemsTotal, nameof(request.ExtraItemsTotal));

        var freeMiles = EnsureNonNegative(settings.PricingFreeMiles, nameof(settings.PricingFreeMiles));
        var billableMiles = RoundMoney(Math.Max(0m, milesBC - freeMiles), roundingMode);
        var freeMilesApplied = RoundMoney(Math.Min(freeMiles, milesBC), roundingMode);

        var serviceProfile = await ResolveServiceProfileAsync(request, cancellationToken);
        InsuranceAccountServiceRate? accountServiceRow = null;
        if (account != null && serviceProfile != null)
        {
            accountServiceRow = await _context.InsuranceAccountServiceRates.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.InsuranceAccountId == account.Id && x.ServicePricingProfileId == serviceProfile.Id,
                    cancellationToken);
        }

        decimal basePrice;
        decimal rateABCatalog;
        decimal rateBCCatalog;
        decimal rateCACatalog;

        if (accountServiceRow != null && serviceProfile != null)
        {
            basePrice = accountServiceRow.BasePrice;
            rateBCCatalog = accountServiceRow.LoadedPricePerMile ?? accountServiceRow.PricePerMile;
            rateABCatalog = accountServiceRow.EnroutePricePerMile ?? serviceProfile.EnroutePricePerMile ?? 0m;
            rateCACatalog = accountServiceRow.DeadheadPricePerMile ?? serviceProfile.DeadheadPricePerMile ?? 0m;
        }
        else if (serviceProfile != null)
        {
            basePrice = serviceProfile.BasePrice;
            rateABCatalog = serviceProfile.EnroutePricePerMile ?? 0m;
            rateBCCatalog = serviceProfile.LoadedPricePerMile ?? serviceProfile.PricePerMile;
            rateCACatalog = serviceProfile.DeadheadPricePerMile ?? 0m;
        }
        else
        {
            // Legacy: account-level BC rate when no service catalog match
            basePrice = 0m;
            rateABCatalog = 0m;
            rateBCCatalog = account?.RateBC ?? 0m;
            rateCACatalog = 0m;
        }

        var catalogPricing = accountServiceRow != null || serviceProfile != null;
        decimal hookupLineAmount;
        var hookupLineApplies = false;
        if (catalogPricing)
        {
            // Towing base is BasePrice (+ mileage); optional hookup is dispatcher-entered via request only.
            hookupLineAmount = 0m;
            hookupLineApplies = false;
        }
        else
        {
            // Legacy: no service catalog; hookup line uses system default (Admin → Settings) only.
            hookupLineAmount = settings.DefaultPricingHookupFee;
            hookupLineApplies = hookupLineAmount > 0m;
        }

        var rateAB = rateABCatalog;
        var rateBC = rateBCCatalog;
        var rateCA = rateCACatalog;

        // Dispatcher overrides (explicit line items from UI)
        if (request.RateAB.HasValue)
        {
            rateAB = EnsureNonNegative(request.RateAB.Value, nameof(request.RateAB));
        }

        if (request.RateBC.HasValue)
        {
            rateBC = EnsureNonNegative(request.RateBC.Value, nameof(request.RateBC));
        }

        if (request.RateCA.HasValue)
        {
            rateCA = EnsureNonNegative(request.RateCA.Value, nameof(request.RateCA));
        }

        if (request.HookupFee.HasValue)
        {
            hookupLineAmount = EnsureNonNegative(request.HookupFee.Value, nameof(request.HookupFee));
            hookupLineApplies = hookupLineAmount > 0m;
        }

        basePrice = EnsureNonNegative(basePrice, nameof(basePrice));
        rateAB = EnsureNonNegative(rateAB, nameof(rateAB));
        rateBC = EnsureNonNegative(rateBC, nameof(rateBC));
        rateCA = EnsureNonNegative(rateCA, nameof(rateCA));
        hookupLineAmount = EnsureNonNegative(hookupLineAmount, nameof(hookupLineAmount));

        var hookupLineCharge = hookupLineApplies ? hookupLineAmount : 0m;
        hookupLineCharge = RoundMoney(hookupLineCharge, roundingMode);

        var chargeAB = RoundMoney(milesAB * rateAB, roundingMode);
        var chargeBC = RoundMoney(billableMiles * rateBC, roundingMode);
        var chargeCA = RoundMoney(milesCA * rateCA, roundingMode);
        rateAB = RoundMoney(rateAB, roundingMode);
        rateBC = RoundMoney(rateBC, roundingMode);
        rateCA = RoundMoney(rateCA, roundingMode);

        var hookupFeeTotal = hookupLineCharge;

        var serviceChargePercent = request.ServiceChargePercent
            ?? settings.DefaultPricingServiceChargePercent;
        var taxPercent = request.TaxPercent
            ?? settings.DefaultPricingTaxPercent;

        serviceChargePercent = EnsurePercent(serviceChargePercent, nameof(request.ServiceChargePercent));
        taxPercent = EnsurePercent(taxPercent, nameof(request.TaxPercent));

        // Match dispatcher UI: fixed "best" price is carried as invoice line items (ExtraItemsTotal, e.g. "Name (base)").
        // Do not also add catalog BasePrice when those lines already subsume it.
        var monetaryBaseComponent = ComputeMonetaryBaseComponent(basePrice, extraItemsTotal, roundingMode);
        var baseSubtotal = RoundMoney(
            monetaryBaseComponent + chargeAB + chargeBC + chargeCA + hookupFeeTotal,
            roundingMode);

        var discountAmount = ResolveDiscountAmount(request, baseSubtotal, settings.MaxDiscountPercent, roundingMode);
        var afterDiscount = RoundMoney(Math.Max(0m, baseSubtotal - discountAmount), roundingMode);

        var serviceChargeAmount = RoundMoney(afterDiscount * (serviceChargePercent / 100m), roundingMode);
        var taxableAmount = RoundMoney(afterDiscount + serviceChargeAmount, roundingMode);

        var taxExempt = request.TaxExempt ?? false;
        var taxAmount = taxExempt ? 0m : RoundMoney(taxableAmount * (taxPercent / 100m), roundingMode);
        var computedGrandTotal = RoundMoney(taxableAmount + taxAmount, roundingMode);

        var manualApplied = false;
        var grandTotal = computedGrandTotal;
        if (request.ManualTotalOverride.HasValue)
        {
            if (!settings.AllowManualTotalOverride)
            {
                throw new InvalidOperationException("Manual total override is disabled by policy.");
            }

            if (settings.ManualOverrideRequiresReason &&
                string.IsNullOrWhiteSpace(request.ManualOverrideReason))
            {
                throw new InvalidOperationException("Manual total override reason is required.");
            }

            manualApplied = true;
            grandTotal = RoundMoney(EnsureNonNegative(request.ManualTotalOverride.Value, nameof(request.ManualTotalOverride)), roundingMode);
        }

        return new PricingQuoteResponseDto
        {
            AccountId = account?.Id,
            AccountName = account?.Name,
            MilesAB = milesAB,
            MilesBC = milesBC,
            MilesCA = milesCA,
            BillableMiles = billableMiles,
            FreeMilesApplied = freeMilesApplied,
            PricingFreeMilesAllowance = freeMiles,
            ServiceBasePrice = RoundMoney(basePrice, roundingMode),
            HookupFee = hookupFeeTotal,
            RateAB = rateAB,
            RateBC = rateBC,
            RateCA = rateCA,
            ChargeAB = chargeAB,
            ChargeBC = chargeBC,
            ChargeCA = chargeCA,
            ExtraItemsTotal = RoundMoney(extraItemsTotal, roundingMode),
            BaseSubtotal = baseSubtotal,
            DiscountAmount = discountAmount,
            AfterDiscount = afterDiscount,
            ServiceChargePercent = RoundMoney(serviceChargePercent, roundingMode),
            ServiceChargeAmount = serviceChargeAmount,
            TaxExempt = taxExempt,
            TaxPercent = RoundMoney(taxPercent, roundingMode),
            TaxableAmount = taxableAmount,
            TaxAmount = taxAmount,
            GrandTotal = grandTotal,
            ManualTotalOverrideApplied = manualApplied,
            ManualTotalOverride = request.ManualTotalOverride,
            ManualOverrideReason = request.ManualOverrideReason?.Trim(),
            MaxDiscountPercent = settings.MaxDiscountPercent,
            AllowManualTotalOverride = settings.AllowManualTotalOverride,
            ManualOverrideRequiresReason = settings.ManualOverrideRequiresReason,
            PricingMismatchTolerance = settings.PricingMismatchTolerance,
            PricingRoundingMode = roundingMode.ToString()
        };
    }

    private async Task<InsuranceAccount?> ResolveAccountAsync(PricingQuoteRequestDto request, CancellationToken cancellationToken)
    {
        if (request.AccountId.HasValue)
        {
            return await _context.InsuranceAccounts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.AccountId.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.AccountName))
        {
            var normalized = request.AccountName.Trim();
            return await _context.InsuranceAccounts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Name == normalized, cancellationToken);
        }

        return null;
    }

    private async Task<ServicePricingProfile?> ResolveServiceProfileAsync(
        PricingQuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.ServicePricingProfileId.HasValue)
        {
            return await _context.ServicePricingProfiles.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.ServicePricingProfileId.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.ServiceName))
        {
            var n = request.ServiceName.Trim();
            return await _context.ServicePricingProfiles.AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Name.ToLower() == n.ToLower(),
                    cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Single monetary base for subtotal: catalog fixed price and/or line-item extras, without double-counting
    /// when the UI sends the catalog base again as part of <see cref="PricingQuoteRequestDto.ExtraItemsTotal"/>.
    /// </summary>
    private static decimal ComputeMonetaryBaseComponent(
        decimal catalogBasePrice,
        decimal extraItemsTotal,
        MidpointRounding roundingMode)
    {
        catalogBasePrice = RoundMoney(EnsureNonNegative(catalogBasePrice, nameof(catalogBasePrice)), roundingMode);
        extraItemsTotal = RoundMoney(EnsureNonNegative(extraItemsTotal, nameof(extraItemsTotal)), roundingMode);

        const decimal tolerance = 0.01m;
        if (catalogBasePrice > 0m && extraItemsTotal + tolerance >= catalogBasePrice)
        {
            return extraItemsTotal;
        }

        return RoundMoney(catalogBasePrice + extraItemsTotal, roundingMode);
    }

    private static decimal ResolveDiscountAmount(
        PricingQuoteRequestDto request,
        decimal baseSubtotal,
        decimal maxDiscountPercent,
        MidpointRounding roundingMode)
    {
        var explicitAmount = EnsureNonNegative(request.DiscountAmount, nameof(request.DiscountAmount));
        var percent = request.DiscountPercent.HasValue
            ? EnsurePercent(request.DiscountPercent.Value, nameof(request.DiscountPercent))
            : 0m;

        var amountFromPercent = percent <= 0 ? 0m : baseSubtotal * (percent / 100m);
        var desiredAmount = explicitAmount > 0 ? explicitAmount : amountFromPercent;
        var allowedMax = baseSubtotal * (Math.Min(100m, Math.Max(0m, maxDiscountPercent)) / 100m);
        return RoundMoney(Math.Min(desiredAmount, allowedMax), roundingMode);
    }

    private static decimal EnsureNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
        {
            throw new InvalidOperationException($"{fieldName} cannot be negative.");
        }

        return value;
    }

    private static decimal EnsurePercent(decimal value, string fieldName)
    {
        if (value < 0 || value > 100)
        {
            throw new InvalidOperationException($"{fieldName} must be between 0 and 100.");
        }

        return value;
    }

    private static decimal RoundMoney(decimal value, MidpointRounding roundingMode)
    {
        return decimal.Round(value, 2, roundingMode);
    }

    private static MidpointRounding ResolveRoundingMode(string? mode)
    {
        return string.Equals(mode, "ToEven", StringComparison.OrdinalIgnoreCase)
            ? MidpointRounding.ToEven
            : MidpointRounding.AwayFromZero;
    }
}
