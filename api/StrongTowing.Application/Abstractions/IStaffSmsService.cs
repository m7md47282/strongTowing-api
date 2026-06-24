using StrongTowing.Application.DTOs.Responses;

namespace StrongTowing.Application.Abstractions;

/// <summary>
/// Sends one-off SMS messages initiated by staff (quotes, invoices) using Twilio credentials from system settings.
/// </summary>
public interface IStaffSmsService
{
    Task<StaffSmsResponse> SendAsync(string toPhone, string body, CancellationToken cancellationToken = default);
}
