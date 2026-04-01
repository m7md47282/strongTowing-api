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

        var hookupFee = request.HookupFee
            ?? account?.HookupFee
            ?? settings.DefaultPricingHookupFee;
        var rateAB = request.RateAB
            ?? account?.RateAB
            ?? 0m;
        var rateBC = request.RateBC
            ?? account?.RateBC
            ?? 0m;
        var rateCA = request.RateCA
            ?? account?.RateCA
            ?? 0m;
        var serviceChargePercent = request.ServiceChargePercent
            ?? account?.ServiceChargePercent
            ?? settings.DefaultPricingServiceChargePercent;
        var taxPercent = request.TaxPercent
            ?? account?.TaxPercent
            ?? settings.DefaultPricingTaxPercent;

        hookupFee = EnsureNonNegative(hookupFee, nameof(request.HookupFee));
        rateAB = EnsureNonNegative(rateAB, nameof(request.RateAB));
        rateBC = EnsureNonNegative(rateBC, nameof(request.RateBC));
        rateCA = EnsureNonNegative(rateCA, nameof(request.RateCA));
        serviceChargePercent = EnsurePercent(serviceChargePercent, nameof(request.ServiceChargePercent));
        taxPercent = EnsurePercent(taxPercent, nameof(request.TaxPercent));

        var chargeAB = RoundMoney(milesAB * rateAB, roundingMode);
        var chargeBC = RoundMoney(milesBC * rateBC, roundingMode);
        var chargeCA = RoundMoney(milesCA * rateCA, roundingMode);

        var baseSubtotal = RoundMoney(hookupFee + chargeAB + chargeBC + chargeCA + extraItemsTotal, roundingMode);

        var discountAmount = ResolveDiscountAmount(request, baseSubtotal, settings.MaxDiscountPercent, roundingMode);
        var afterDiscount = RoundMoney(Math.Max(0m, baseSubtotal - discountAmount), roundingMode);

        var serviceChargeAmount = RoundMoney(afterDiscount * (serviceChargePercent / 100m), roundingMode);
        var taxableAmount = RoundMoney(afterDiscount + serviceChargeAmount, roundingMode);

        var taxExempt = request.TaxExempt ?? account?.IsTaxExemptByDefault ?? false;
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
            HookupFee = RoundMoney(hookupFee, roundingMode),
            RateAB = RoundMoney(rateAB, roundingMode),
            RateBC = RoundMoney(rateBC, roundingMode),
            RateCA = RoundMoney(rateCA, roundingMode),
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
