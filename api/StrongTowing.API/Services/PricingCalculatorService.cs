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
        decimal pricePerMile;
        var hookEnabled = false;
        decimal hookAmount;

        if (accountServiceRow != null)
        {
            basePrice = accountServiceRow.BasePrice;
            pricePerMile = accountServiceRow.PricePerMile;
            hookEnabled = accountServiceRow.HookFeeEnabled;
            hookAmount = accountServiceRow.HookFeeAmount;
        }
        else if (serviceProfile != null)
        {
            basePrice = serviceProfile.BasePrice;
            pricePerMile = serviceProfile.PricePerMile;
            hookEnabled = serviceProfile.HookFeeEnabled;
            hookAmount = serviceProfile.HookFeeAmount;
        }
        else
        {
            // Legacy: account-level BC rate + default/account hookup when no service catalog match
            basePrice = 0m;
            pricePerMile = account?.RateBC ?? 0m;
            hookAmount = account?.HookupFee ?? settings.DefaultPricingHookupFee;
            hookEnabled = hookAmount > 0m;
        }

        // Dispatcher overrides (explicit line items from UI)
        if (request.RateBC.HasValue)
        {
            pricePerMile = EnsureNonNegative(request.RateBC.Value, nameof(request.RateBC));
        }

        if (request.HookupFee.HasValue)
        {
            hookAmount = EnsureNonNegative(request.HookupFee.Value, nameof(request.HookupFee));
            hookEnabled = hookAmount > 0m;
        }

        basePrice = EnsureNonNegative(basePrice, nameof(basePrice));
        pricePerMile = EnsureNonNegative(pricePerMile, nameof(pricePerMile));
        hookAmount = EnsureNonNegative(hookAmount, nameof(hookAmount));

        var hookCharge = hookEnabled ? hookAmount : 0m;
        hookCharge = RoundMoney(hookCharge, roundingMode);

        // Customer pays loaded miles only; enroute and deadhead are not billed
        var chargeAB = 0m;
        var chargeCA = 0m;
        var rateAB = 0m;
        var rateCA = 0m;

        var chargeBC = RoundMoney(billableMiles * pricePerMile, roundingMode);
        var rateBC = RoundMoney(pricePerMile, roundingMode);

        var hookupFeeTotal = hookCharge;

        var serviceChargePercent = request.ServiceChargePercent
            ?? settings.DefaultPricingServiceChargePercent;
        var taxPercent = request.TaxPercent
            ?? settings.DefaultPricingTaxPercent;

        serviceChargePercent = EnsurePercent(serviceChargePercent, nameof(request.ServiceChargePercent));
        taxPercent = EnsurePercent(taxPercent, nameof(request.TaxPercent));

        var baseSubtotal = RoundMoney(basePrice + chargeBC + hookupFeeTotal + extraItemsTotal, roundingMode);

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
