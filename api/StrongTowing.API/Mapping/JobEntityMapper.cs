using System.Text.Json;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;

namespace StrongTowing.API.Mapping;

/// <summary>
/// Maps <see cref="Job"/> entities to <see cref="JobDto"/> for API responses (shared by JobsController and DashboardController).
/// </summary>
public static class JobEntityMapper
{
    public static JobDto MapToDto(Job job, decimal? driverCommissionPct = null)
    {
        InvoiceChargesData? invoiceCharges = null;
        if (!string.IsNullOrEmpty(job.InvoiceChargesJson))
        {
            try
            {
                invoiceCharges = JsonSerializer.Deserialize<InvoiceChargesData>(job.InvoiceChargesJson);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new JobDto
        {
            Id = job.Id,
            Status = job.Status.ToString(),
            VehicleId = job.VehicleId,
            Vehicle = job.Vehicle != null ? new VehicleDto
            {
                Id = job.Vehicle.Id,
                VIN = job.Vehicle.VIN,
                Make = job.Vehicle.Make,
                Model = job.Vehicle.Model,
                Year = job.Vehicle.Year,
                Color = job.Vehicle.Color
            } : null,
            ClientId = job.Vehicle?.OwnerId ?? string.Empty,
            ClientName = job.Vehicle?.Owner?.FullName ?? string.Empty,
            ClientEmail = job.Vehicle?.Owner?.Email ?? string.Empty,
            ClientPhoneNumber = job.Vehicle?.Owner?.PhoneNumber,

            CallType = job.CallType,
            ScheduledDate = job.ScheduledDate,
            ScheduledTime = job.ScheduledTime,

            CompanyName = job.CompanyName,
            Account = job.Account,
            CompanyOverride = job.CompanyOverride,

            ContactName = job.ContactName,
            ContactPhoneNumber = job.ContactPhoneNumber,

            PickupLocation = job.PickupLocation,
            DestinationAddress = job.DestinationAddress,

            Reason = job.Reason,
            Priority = job.Priority,
            InvoiceNumber = job.InvoiceNumber,
            ETA = job.ETA,
            ServiceType = job.ServiceType,

            LicensePlate = job.LicensePlate,
            LicenseState = job.LicenseState,
            DriveType = job.DriveType,
            VehicleType = job.VehicleType,
            Odometer = job.Odometer,
            Drivable = job.Drivable,
            HaveKeys = job.HaveKeys,
            KeyLocation = job.KeyLocation,

            DriverId = job.DriverId,
            DriverName = job.Driver?.FullName,
            TruckId = job.TruckId,
            Truck = job.Truck == null
                ? null
                : new TruckSummaryDto
                {
                    Id = job.Truck.Id,
                    UnitLabel = job.Truck.UnitLabel,
                    TruckTypeId = job.Truck.TruckTypeId,
                    TruckTypeName = job.Truck.TruckType?.Name ?? string.Empty
                },

            Cost = job.Cost,
            CommissionVisibleToDriver = job.CommissionVisibleToDriver,
            DriverCommissionRatePercent = driverCommissionPct.HasValue && job.CommissionVisibleToDriver
                ? driverCommissionPct
                : null,
            DriverCommissionEstimate = driverCommissionPct.HasValue && job.CommissionVisibleToDriver
                ? decimal.Round(job.Cost * (driverCommissionPct.Value / 100m), 2, MidpointRounding.AwayFromZero)
                : null,
            PaymentStatus = string.IsNullOrWhiteSpace(job.PaymentStatus) ? "Unpaid" : job.PaymentStatus,
            PaymentMethod = job.PaymentMethod,
            PaidAt = job.PaidAt,
            BillingPaymentMode = string.IsNullOrWhiteSpace(job.BillingPaymentMode) ? JobBillingModes.Standard : job.BillingPaymentMode,
            InsuranceCoveredAmount = job.InsuranceCoveredAmount,
            ClientCoveredAmount = job.ClientCoveredAmount,
            InsurancePortionBilled = job.InsurancePortionBilled,
            ClientPortionPaid = job.ClientPortionPaid,
            DriverCashCollectedAmount = job.DriverCashCollectedAmount,
            PayrollDeductionAmount = job.PayrollDeductionAmount,
            PayrollDeductionRecorded = job.PayrollDeductionRecorded,
            Notes = job.Notes,
            BillingNotes = job.BillingNotes,
            IncludeBillingNotesOnReceipt = job.IncludeBillingNotesOnReceipt,
            InvoiceCharges = invoiceCharges,

            PhotoCount = job.Photos?.Count ?? 0,
            Photos = job.Photos == null || job.Photos.Count == 0
                ? new List<JobPhotoDto>()
                : job.Photos.OrderBy(p => p.UploadedAt).Select(p => new JobPhotoDto
                {
                    Id = p.Id,
                    Url = p.PhotoUrl,
                    UploadedAt = p.UploadedAt
                }).ToList(),
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            StatusUpdatedById = job.StatusUpdatedById,
            StatusUpdatedByName = job.StatusUpdatedBy?.FullName,
            StatusUpdatedAt = job.StatusUpdatedAt
        };
    }
}
