# Pricing Calculation Guide

## Overview

All pricing in the system — what the dispatcher sees in the job form, what appears
on the quote modal, what is printed on the quote PDF, and what is saved as
`Job.Cost` — originates from a **single server-side calculator**:

```
PricingCalculatorService.CalculateAsync(PricingQuoteRequestDto)
  → PricingQuoteResponseDto
```

The frontend is a **pure renderer** of that response. It performs no arithmetic.
There is no fallback math in the browser. If no server quote is available,
no prices are shown and the job cannot be submitted.

---

## Data Flow

```
Dispatcher fills in form
        │
        ▼ (debounced ~400 ms after any change)
POST /api/pricing/quote
        │
        ▼
PricingCalculatorService.CalculateAsync
        │
        ▼
PricingQuoteResponseDto  (latestQuote)
        │
        ├─► Itemised breakdown panel (form)
        ├─► Read-only Cost display (form)
        ├─► Quote review modal totals
        ├─► Quote PDF line items
        └─► Quote PDF subtotal / tax / grand total

Dispatcher clicks Create Job
        │
        └─► cost = latestQuote.grandTotal
                │
                ▼
            JobsController.CreateJob
              re-runs PricingCalculatorService
              rejects if |submitted − calculated| > PricingMismatchTolerance
```

---

## Inputs to the Calculator

| Input | Source | Notes |
|---|---|---|
| `AccountId` / `AccountName` | Form account selector | Resolves account-level rates |
| `ServicePricingProfileId` / `ServiceName` | Form service type | Resolves catalog rates |
| `MilesAB` | Google Maps (office → pickup) | Enroute miles |
| `MilesBC` | Google Maps (pickup → destination) | Loaded miles |
| `MilesCA` | Google Maps (destination → office) | Deadhead miles |
| `RateAB` / `RateBC` / `RateCA` | Form override fields (optional) | Overrides catalog rates when set |
| `HookupFee` | Form override field (optional) | Overrides default when set |
| `ExtraItemsTotal` | Sum of `invoiceServiceItems` | Winch, storage, labor, etc. |
| `DiscountAmount` | Form flat-discount field | Used if > 0 |
| `DiscountPercent` | Form percent-discount field | Used when `DiscountAmount == 0` |
| `ServiceChargePercent` | Form field (optional) | Falls back to system setting |
| `TaxPercent` | Form field (optional) | Falls back to system setting |
| `TaxExempt` | Form toggle | |
| `ManualTotalOverride` | Form override field (optional) | Replaces computed grand total |
| `ManualOverrideReason` | Form text field | Required if policy enforces it |

---

## Rate Resolution Order

The calculator resolves rates in this priority order for each leg:

1. **Dispatcher override** — explicit `RateAB` / `RateBC` / `RateCA` / `HookupFee`
   values sent from the form.
2. **Account × Service profile row** (`InsuranceAccountServiceRates`) — when the
   selected account has a specific rate row for the selected service profile.
3. **Service pricing profile** (`ServicePricingProfiles`) — catalog base rate.
4. **Account-level BC rate** — legacy fallback when no service profile matches;
   only `RateBC` is available, `RateAB` and `RateCA` default to 0.

A `BasePrice` from the service profile (or account service row) feeds into
`ExtraItemsTotal` via `ComputeMonetaryBaseComponent`, which prevents
double-counting when the dispatcher has already sent the base as a line item.

---

## Calculation Steps

### Step 1 — Free miles

```
BillableMiles = max(0, MilesBC − PricingFreeMiles)
FreeMilesApplied = min(PricingFreeMiles, MilesBC)
```

`PricingFreeMiles` comes from system settings (`Admin → Settings`).

### Step 2 — Mileage charges

```
ChargeAB = MilesAB × RateAB
ChargeBC = BillableMiles × RateBC
ChargeCA = MilesCA × RateCA
```

### Step 3 — Base subtotal

```
MonetaryBase = (catalogBasePrice > 0 && ExtraItemsTotal ≥ catalogBasePrice)
               ? ExtraItemsTotal          ← extras already include the base
               : catalogBasePrice + ExtraItemsTotal

BaseSubtotal = MonetaryBase + ChargeAB + ChargeBC + ChargeCA + HookupFee
```

### Step 4 — Discount

Only one discount mode is used per call:

```
DiscountFromPercent = BaseSubtotal × (DiscountPercent / 100)
DesiredDiscount     = DiscountAmount > 0 ? DiscountAmount : DiscountFromPercent
AllowedMax          = BaseSubtotal × (MaxDiscountPercent / 100)   ← from system settings
DiscountAmount      = min(DesiredDiscount, AllowedMax)

AfterDiscount = max(0, BaseSubtotal − DiscountAmount)
```

### Step 5 — Service charge

```
ServiceChargeAmount = AfterDiscount × (ServiceChargePercent / 100)
```

### Step 6 — Taxable amount

```
TaxableAmount = AfterDiscount + ServiceChargeAmount
```

### Step 7 — Tax

```
TaxAmount = TaxExempt ? 0 : TaxableAmount × (TaxPercent / 100)
```

### Step 8 — Grand total

```
ComputedGrandTotal = TaxableAmount + TaxAmount
```

### Step 9 — Manual override (if enabled by policy)

```
GrandTotal = ManualTotalOverride ?? ComputedGrandTotal
```

`AllowManualTotalOverride` and `ManualOverrideRequiresReason` are system-settings
flags. If the override is applied, `ManualTotalOverrideApplied = true` is set in
the response and the audit fields (`AdjustedBy`, `AdjustedAt`, `AdjustmentReason`)
are written to the job's `InvoiceChargesJson`.

### Step 10 — Rounding

All monetary values are rounded to **2 decimal places** using the mode
configured in `SystemSettings.PricingRoundingMode`:

- `AwayFromZero` (default) — 2.345 → 2.35
- `ToEven` (banker's rounding) — 2.345 → 2.34

---

## Full Formula Reference

```
BillableMiles      = max(0, MilesBC − FreeMiles)
ChargeAB           = round(MilesAB × RateAB)
ChargeBC           = round(BillableMiles × RateBC)
ChargeCA           = round(MilesCA × RateCA)
BaseSubtotal       = round(MonetaryBase + ChargeAB + ChargeBC + ChargeCA + HookupFee)
DiscountAmount     = round(min(desired, BaseSubtotal × MaxDiscountPercent%))
AfterDiscount      = round(max(0, BaseSubtotal − DiscountAmount))
ServiceCharge      = round(AfterDiscount × ServiceChargePercent%)
TaxableAmount      = round(AfterDiscount + ServiceCharge)
TaxAmount          = TaxExempt ? 0 : round(TaxableAmount × TaxPercent%)
GrandTotal         = ManualOverride ?? round(TaxableAmount + TaxAmount)
```

---

## Worked Example

**Inputs**

| Field | Value |
|---|---|
| HookupFee | $75.00 |
| MilesAB | 12.4 mi @ $7.00/mi |
| MilesBC | 8.0 mi @ $9.00/mi (0 free miles) |
| MilesCA | 10.2 mi @ $3.50/mi |
| ExtraItems | $25.00 (winch labor) |
| Discount | $10.00 flat |
| ServiceCharge | 5% |
| Tax | 10% |
| TaxExempt | No |

**Computation**

```
ChargeAB      = 12.4 × 7.00          = $86.80
ChargeBC      = 8.0  × 9.00          = $72.00
ChargeCA      = 10.2 × 3.50          = $35.70
BaseSubtotal  = 75 + 86.80 + 72 + 35.70 + 25 = $294.50
DiscountAmt   = $10.00
AfterDiscount = 294.50 − 10.00       = $284.50
ServiceCharge = 284.50 × 0.05        = $14.23
TaxableAmount = 284.50 + 14.23       = $298.73
Tax           = 298.73 × 0.10        = $29.87
GrandTotal    = 298.73 + 29.87       = $328.60
```

**What the dispatcher sees, what is on the PDF, and what is saved as `Job.Cost`:**
**$328.60** — all three are the same number from the same calculator.

---

## PDF / Quote Line Items

The quote PDF is built from `PricingQuoteResponseDto` only. Each visible row maps
to exactly one field from the server response:

| Row (shown only when value > 0) | Source field |
|---|---|
| Service Base | `serviceBasePrice` |
| Hookup Fee | `hookupFee` |
| Enroute Mileage | `milesAB × rateAB = chargeAB` |
| Loaded Mileage | `billableMiles × rateBC = chargeBC` + free-miles note |
| Deadhead Mileage | `milesCA × rateCA = chargeCA` |
| Extra line items | `invoiceServiceItems` (one row each) |
| **Subtotal** | `baseSubtotal` |
| − Discount | `discountAmount` |
| After Discount | `afterDiscount` |
| + Service Charge (X%) | `serviceChargeAmount` |
| Taxable Amount | `taxableAmount` |
| + Tax (X% / exempt) | `taxAmount` |
| **TOTAL ESTIMATE** | `grandTotal` |

The visible rows sum to `baseSubtotal`. Applying the footer steps
(discount → service charge → tax) always produces `grandTotal`. There is no
rounding gap because every value comes from the same already-rounded server
response.

---

## Dispatcher Overrides

Dispatchers can change any input that feeds the calculator. Changing any of these
triggers an automatic debounced recalc (`POST /api/pricing/quote`):

- Mileage quantities and per-mile rates for each leg
- Hookup fee
- Extra service line items (name, qty, price)
- Discount (flat $ or %)
- Service charge %
- Tax %
- Tax-exempt toggle
- Manual Total Override + reason

**Important:** the dispatcher edits *inputs*, not outputs. The server
always computes the output. The "Cost" field and all summary totals are
read-only displays of `latestQuote.grandTotal`.

---

## Job Submission and Audit Trail

When the dispatcher creates a job:

1. The frontend sends `cost = latestQuote.grandTotal` and invoice charge fields
   that mirror the server response exactly.
2. `JobsController.CreateJob` re-runs `PricingCalculatorService.CalculateAsync`
   with the same inputs.
3. If `|submitted − recomputed| > PricingMismatchTolerance` the request is
   rejected with HTTP 400.
4. `Job.Cost` is set to `pricingQuote.GrandTotal` (the server's own value, not
   the submitted value).
5. The full pricing snapshot is serialised to `Job.InvoiceChargesJson`:

   - All miles and rates used
   - Charges per leg
   - Extra items total
   - Base subtotal
   - Discount details
   - Service charge amount
   - Tax amount and exempt flag
   - Final grand total
   - Override metadata if applicable

This snapshot is immutable after creation. Future changes to account rates or
system settings do not affect historical job costs.

---

## System Settings That Affect Pricing

| Setting | Effect |
|---|---|
| `PricingFreeMiles` | Free loaded miles deducted from `MilesBC` before billing |
| `DefaultPricingHookupFee` | Default hookup fee when no service profile is matched |
| `DefaultPricingServiceChargePercent` | Default service charge % when not overridden |
| `DefaultPricingTaxPercent` | Default tax % when not overridden |
| `MaxDiscountPercent` | Hard cap on discount as a % of `BaseSubtotal` |
| `AllowManualTotalOverride` | Enables the dispatcher's Manual Total Override input |
| `ManualOverrideRequiresReason` | Enforces a reason string for manual overrides |
| `PricingMismatchTolerance` | Max allowed delta (USD) between submitted and recomputed total |
| `PricingRoundingMode` | `AwayFromZero` or `ToEven` |

---

## API Endpoints

### `POST /api/pricing/quote`
Live preview. Called automatically by the dispatcher form on every input change
(debounced). Returns the full `PricingQuoteResponseDto`. Does not create a job.
Requires `SuperAdmin`, `Administrator`, or `Dispatcher` role.

### `POST /api/jobs`
Authoritative create. Re-runs the calculator server-side and saves
`Job.Cost = pricingQuote.GrandTotal`. Rejects if submitted cost diverges beyond
tolerance.

### `POST /api/jobs/{id}/override-price`
Admin-only post-creation price correction. Writes the new cost and appends an
audit line to `Job.Notes`. Marked as `ManualTotalOverride` in
`InvoiceChargesJson`.

### `POST /api/quotes/email`
Sends the quote to a recipient via Postmark. Renders the PDF server-side
using Playwright so icons and payment logos stay vector.

### `POST /api/quotes/pdf`
Generates a downloadable PDF from the quote data.

---

## Single Source of Truth Rule

> **One calculator. One result. Used everywhere.**

- The frontend never computes a monetary value.
- Every dollar amount visible to the dispatcher or the customer comes from
  `PricingCalculatorService.CalculateAsync`.
- The quote PDF, the form totals, the cost field, the SMS text, and the
  email attachment are all rendered from the same `PricingQuoteResponseDto`
  instance (`latestQuote` in the Angular component).
- If no server quote exists, no prices are shown and no submission is allowed.
