using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;

namespace StrongTowing.Application.Abstractions;

public interface IPricingCalculatorService
{
    Task<PricingQuoteResponseDto> CalculateAsync(
        PricingQuoteRequestDto request,
        CancellationToken cancellationToken = default);
}
