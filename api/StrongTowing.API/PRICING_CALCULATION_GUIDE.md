# Pricing Calculation Guide (Create Job)

## Purpose
This document defines the full towing pricing logic for job creation so calculation rules are centralized, readable, and easy to maintain.

It is designed to support:
- Account-based default pricing profiles
- Automatic route-based mileage calculation
- Per-call adjustments (without changing account defaults)
- Server-side authoritative totals

---

## Pricing Workflow
1. Dispatcher creates a new call and selects an account.
2. System loads account pricing defaults.
3. Dispatcher enters locations:
   - `A` = Company/yard location
   - `B` = Pickup location
   - `C` = Drop-off location
4. System calculates distances:
   - `A -> B` (enroute/unloaded)
   - `B -> C` (loaded/hooked)
   - `C -> A` (dead mileage/return)
5. System applies pricing rates and fees.
6. Dispatcher may apply per-call edits (rate/amount/discount/override).
7. Final total is calculated and saved with the job.
8. Full charge breakdown is stored as an invoice snapshot.

---

## Account Pricing Profile (Default Inputs)
Each account should provide these default pricing values:

- `HookupFee` (fixed amount per call)
- `RateAB` (USD per mile for `A -> B`)
- `RateBC` (USD per mile for `B -> C`)
- `RateCA` (USD per mile for `C -> A`)
- `ServiceChargePercent` (percentage applied after base charges)
- `TaxPercent` (percentage applied on taxable amount)
- Optional rules:
  - `IsTaxExemptByDefault`
  - Min/max caps
  - Rounding policy

---

## Distance Inputs
Distance values used in pricing:

- `MilesAB`: company to pickup
- `MilesBC`: pickup to drop-off
- `MilesCA`: drop-off to company

Distance sources:
- Preferred: server-side map route service
- Acceptable for UI preview: frontend map calculation
- Final pricing should be verified server-side before saving

---

## Line Item Formulas
### Mileage Charges
- `ChargeAB = MilesAB * RateAB`
- `ChargeBC = MilesBC * RateBC`
- `ChargeCA = MilesCA * RateCA`

### Base Subtotal
- `BaseSubtotal = HookupFee + ChargeAB + ChargeBC + ChargeCA + ExtraItemsTotal`

Where `ExtraItemsTotal` includes optional items such as winch, storage, after-hours, labor, etc.

### Discount
Use one discount mode only per call:
- Flat amount discount:
  - `DiscountAmount = EnteredFlatDiscount`
- Percentage discount:
  - `DiscountAmount = BaseSubtotal * (DiscountPercent / 100)`

Then:
- `AfterDiscount = BaseSubtotal - DiscountAmount`
- If negative, clamp to `0`.

### Service Charge
- `ServiceCharge = AfterDiscount * (ServiceChargePercent / 100)`

### Taxable Amount
- `TaxableAmount = AfterDiscount + ServiceCharge`

### Tax
- If tax exempt: `Tax = 0`
- Else: `Tax = TaxableAmount * (TaxPercent / 100)`

### Final Total
- `GrandTotal = TaxableAmount + Tax`

---

## Recommended Rounding Rules
To avoid mismatches between frontend/backend:

- Keep internal calculations at high precision.
- Round only at financial boundaries to 2 decimals:
  - `ChargeAB`, `ChargeBC`, `ChargeCA`
  - `DiscountAmount`
  - `ServiceCharge`
  - `Tax`
  - `GrandTotal`
- Use one shared rounding strategy across all services (AwayFromZero or Bankers, but consistent).

---

## Example Calculation
Inputs:
- `HookupFee = 75.00`
- `MilesAB = 12.4`, `RateAB = 7.00`
- `MilesBC = 8.0`, `RateBC = 9.00`
- `MilesCA = 10.2`, `RateCA = 3.50`
- `ExtraItemsTotal = 25.00`
- `Discount = 10.00` (flat)
- `ServiceChargePercent = 5%`
- `TaxPercent = 10%`
- `TaxExempt = false`

Compute:
- `ChargeAB = 12.4 * 7.00 = 86.80`
- `ChargeBC = 8.0 * 9.00 = 72.00`
- `ChargeCA = 10.2 * 3.50 = 35.70`
- `BaseSubtotal = 75.00 + 86.80 + 72.00 + 35.70 + 25.00 = 294.50`
- `AfterDiscount = 294.50 - 10.00 = 284.50`
- `ServiceCharge = 284.50 * 0.05 = 14.23`
- `TaxableAmount = 284.50 + 14.23 = 298.73`
- `Tax = 298.73 * 0.10 = 29.87`
- `GrandTotal = 298.73 + 29.87 = 328.60`

Final call price: `USD 328.60`

---

## Per-Call Adjustments (Dispatcher Override)
Allowed on the current call only:

- Edit individual rates/amounts
- Add/remove extra service items
- Apply discount
- Tax exempt toggle
- Manual total override (if policy allows)

Required governance:
- Track `AdjustedBy`, `AdjustedAt`, and `AdjustmentReason`.
- Do not mutate account default profile.
- Persist the final resolved values with the job snapshot.

---

## Data to Persist with Job
Store full pricing snapshot with the job (for audit and invoice reproducibility):

- Account identifier + profile version
- A/B/C addresses and resolved miles
- Hookup/rates used
- Extra item lines
- Discount details
- Service charge percent + amount
- Tax percent + amount + tax exempt flag
- Subtotal and final grand total
- Override metadata (if any)

This prevents old jobs from changing if account pricing defaults are edited later.

---

## Validation Rules
- Rates, fees, and miles cannot be negative.
- Discount cannot exceed subtotal.
- Tax percent and service charge percent should be in a valid range (for example `0..100`).
- If frontend total differs from backend recomputation beyond tolerance, backend result wins.
- Job `Cost` should be set from final computed `GrandTotal`.

---

## Implementation Placement (Recommended)
Create a dedicated pricing service:

- `IPricingCalculatorService`
- `PricingCalculatorService`

Service responsibilities:
- Accept normalized pricing input
- Return normalized pricing result + line items
- Apply all calculation and rounding rules in one place

Use this service in:
- Job creation endpoint (`POST /api/jobs`) for authoritative final price
- Optional quote endpoint (`POST /api/pricing/quote`) for live preview

Frontend may calculate preview totals, but backend remains source of truth.

---

## Short Formula Reference
For quick reference:

- `BaseSubtotal = HookupFee + (MilesAB*RateAB) + (MilesBC*RateBC) + (MilesCA*RateCA) + ExtraItemsTotal`
- `AfterDiscount = max(0, BaseSubtotal - DiscountAmount)`
- `ServiceCharge = AfterDiscount * ServiceChargePercent`
- `TaxableAmount = AfterDiscount + ServiceCharge`
- `Tax = TaxExempt ? 0 : TaxableAmount * TaxPercent`
- `GrandTotal = TaxableAmount + Tax`

