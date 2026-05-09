using StrongTowing.Application.DTOs.Requests;

namespace StrongTowing.API.Services;

public interface IQuotePdfGenerator
{
    Task<byte[]> GenerateAsync(QuotePdfRequest request, string assetBaseUrl, CancellationToken cancellationToken = default);
}
