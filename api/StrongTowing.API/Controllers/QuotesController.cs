using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrongTowing.API.Services;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Core.Constants;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotesController : ControllerBase
{
    private readonly IQuotePdfGenerator _quotePdfGenerator;
    private readonly ILogger<QuotesController> _logger;

    public QuotesController(IQuotePdfGenerator quotePdfGenerator, ILogger<QuotesController> logger)
    {
        _quotePdfGenerator = quotePdfGenerator;
        _logger = logger;
    }

    /// <summary>Generates a PDF from the quote payload (Chromium / Playwright).</summary>
    [HttpPost("pdf")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> GeneratePdf([FromBody] QuotePdfRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.QuoteRef))
            return BadRequest(new { error = "quoteRef is required" });

        var assetBase = $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase}".TrimEnd('/');
        try
        {
            var bytes = await _quotePdfGenerator.GenerateAsync(request, assetBase, cancellationToken)
                .ConfigureAwait(false);
            var safeRef = SanitizeFileToken(request.QuoteRef);
            return File(bytes, "application/pdf", $"quote-{safeRef}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quote PDF failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "PDF generation failed. Ensure Playwright browsers are installed on the server (run: pwsh bin/Debug/net9.0/playwright.ps1 install chromium)." });
        }
    }

    private static string SanitizeFileToken(string quoteRef)
    {
        var chars = Path.GetInvalidFileNameChars();
        return string.Join("_", quoteRef.Split(chars, StringSplitOptions.RemoveEmptyEntries)).Trim('_');
    }
}
