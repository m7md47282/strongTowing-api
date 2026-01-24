# Backend Payment System Implementation

## Overview
This document outlines the backend implementation for the payment system. All code has been created and is ready for database migration.

## Created Files

### Entities (StrongTowing.Core/Entities/)
1. **Payment.cs** - Main payment entity
2. **CashCollection.cs** - Cash collection tracking
3. **SystemSettings.cs** - System-wide settings (driver commission, Stripe config)
4. **DriverPayroll.cs** - Bi-weekly payroll records
5. **Job.cs** - Updated with payment fields

### DTOs (StrongTowing.Application/DTOs/)
**Responses:**
- PaymentDto.cs
- PaymentListItemDto.cs
- PaymentStatisticsDto.cs
- SystemSettingsDto.cs

**Requests:**
- ProcessPaymentRequest.cs
- UpdateSystemSettingsRequest.cs

### Controllers (StrongTowing.API/Controllers/)
1. **PaymentsController.cs** - All payment endpoints
2. **SettingsController.cs** - System settings management

### Database Context Updates
- **ApplicationDbContext.cs** - Updated with new DbSets and relationships

## API Endpoints Implemented

### Payments Controller (`/api/payments`)

#### GET /api/payments
- Get all payments with filters
- Query params: `paymentMethod`, `paymentStatus`, `startDate`, `endDate`, `driverId`, `searchTerm`
- Returns: `PaymentListItemDto[]`
- Authorization: Admin/Dispatcher

#### GET /api/payments/{id}
- Get payment by ID
- Returns: `PaymentDto`
- Authorization: Admin/Dispatcher

#### GET /api/payments/job/{jobId}
- Get payment for a specific job
- Returns: `PaymentDto`
- Authorization: Admin/Dispatcher

#### GET /api/payments/statistics
- Get payment statistics
- Query params: `startDate`, `endDate`
- Returns: `PaymentStatisticsDto`
- Authorization: Admin/Dispatcher

#### POST /api/payments
- Process a payment
- Body: `ProcessPaymentRequest`
- Returns: `PaymentDto`
- Authorization: Admin/Dispatcher

### Settings Controller (`/api/settings`)

#### GET /api/settings
- Get system settings
- Returns: `SystemSettingsDto`
- Authorization: Admin/SuperAdmin

#### PUT /api/settings
- Update system settings
- Body: `UpdateSystemSettingsRequest`
- Returns: `SystemSettingsDto`
- Authorization: SuperAdmin only

## Database Migration Required

### New Tables to Create:
1. **Payments**
2. **CashCollections**
3. **SystemSettings**
4. **DriverPayrolls**

### Job Table Updates:
- Add `PaymentMethod` (nullable string)
- Add `PaymentStatus` (string, default 'Unpaid')
- Add `PaymentId` (nullable int, FK to Payments)
- Add `PaidAt` (nullable DateTime)
- Add `PaidBy` (nullable string, User ID)

### Migration Command:
```bash
cd api/StrongTowing.API
dotnet ef migrations add AddPaymentSystem --project ../StrongTowing.Infrastructure
dotnet ef database update --project ../StrongTowing.Infrastructure
```

## Next Steps

1. **Run Database Migration**
   - Execute the migration command above
   - Verify all tables are created correctly

2. **Initialize System Settings**
   - The SettingsController will auto-create default settings on first GET request
   - Or manually insert default row in SystemSettings table

3. **Payment Link Integration** (if needed)
   - Payment links are referenced but entity may need to be created
   - Check existing payment link implementation

4. **Stripe Integration** (future)
   - Add Stripe NuGet package
   - Implement Stripe payment processing
   - Add Stripe secret key to appsettings (encrypted)

5. **Testing**
   - Test all payment endpoints
   - Test settings endpoints
   - Verify commission calculations
   - Test cash collection flow

## Notes

- Commission percentage defaults to 30% if settings don't exist
- Payment status flow: Unpaid → Pending → Paid
- Cash collections are tracked separately and linked to payments
- Driver payroll will be calculated bi-weekly (implementation pending)

## Entity Relationships

```
Job (1) ──→ (0..1) Payment
Payment (1) ──→ (0..1) CashCollection
CashCollection (N) ──→ (1) Driver
DriverPayroll (N) ──→ (1) Driver
SystemSettings (1) - Singleton table
```

## Commission Calculation

Driver commission is calculated as:
```
Commission = Job Cost × (Commission Percentage / 100)
```

Example:
- Job Cost: $150
- Commission %: 30%
- Driver Commission: $150 × 0.30 = $45

## Cash Collection Flow

1. Payment created with PaymentMethod = "Cash"
2. CashCollection record created when driver collects
3. Cash amount deducted from driver's bi-weekly payroll
4. Net pay = Gross Commission - Cash Collected
