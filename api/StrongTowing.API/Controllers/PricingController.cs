using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrongTowing.Application.Abstractions;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
public class PricingController : ControllerBase
{
    private readonly IPricingCalculatorService _pricingCalculator;
    private readonly ILogger<PricingController> _logger;

    public PricingController(IPricingCalculatorService pricingCalculator, ILogger<PricingController> logger)
    {
        _pricingCalculator = pricingCalculator;
        _logger = logger;
    }

    [HttpPost("quote")]
    public async Task<ActionResult<PricingQuoteResponseDto>> Quote(
        [FromBody] PricingQuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pricingCalculator.CalculateAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "Bad Request", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating pricing quote");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while calculating pricing." });
        }
    }
}
