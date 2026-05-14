using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.API.Services;
using StrongTowing.Application.Abstractions;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotesController : ControllerBase
{
    private readonly IQuotePdfGenerator _quotePdfGenerator;
    private readonly ApplicationDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<QuotesController> _logger;

    public QuotesController(
        IQuotePdfGenerator quotePdfGenerator,
        ApplicationDbContext db,
        IEncryptionService encryption,
        IEmailSender emailSender,
        ILogger<QuotesController> logger)
    {
        _quotePdfGenerator = quotePdfGenerator;
        _db = db;
        _encryption = encryption;
        _emailSender = emailSender;
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
            var detail = ex.GetBaseException().Message;
            if (detail.Length > 500)
                detail = detail[..500] + "…";

            var rid = RuntimeInformation.RuntimeIdentifier;
            var nodeFolder = Path.Combine(AppContext.BaseDirectory, ".playwright", "node");
            var availableNodeRids = Directory.Exists(nodeFolder)
                ? string.Join(",", Directory.GetDirectories(nodeFolder).Select(Path.GetFileName))
                : "(missing)";
            var hostNodeFolder = OperatingSystem.IsWindows()
                ? "win32_x64"
                : OperatingSystem.IsMacOS()
                    ? (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "darwin-arm64" : "darwin-x64")
                    : (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64");
            var ridMismatch = !availableNodeRids.Split(',').Contains(hostNodeFolder);

            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new
                {
                    error =
                        "PDF generation failed. Chromium is installed under ContentRoot/.pw-browsers (HTTPS outbound required once). On Linux you may need OS libraries (see Playwright docs: install-deps). Set PLAYWRIGHT_FORCE_NO_SANDBOX=1 if launch is blocked.",
                    detail,
                    diagnostics = new
                    {
                        runtimeIdentifier = rid,
                        expectedNodeFolder = hostNodeFolder,
                        availableNodeFolders = availableNodeRids,
                        ridMismatch,
                        hint = ridMismatch
                            ? $"The published .playwright/node folder has '{availableNodeRids}' but this host needs '{hostNodeFolder}'. Republish with `dotnet publish -r {rid} -p:PlaywrightPlatform=win` (or use the IIS publish profile)."
                            : null
                    }
                });
        }
    }

    /// <summary>
    /// Sends a service quote to a recipient via Postmark using settings-managed credentials.
    /// The frontend renders the PDF locally and forwards it as base64 (<see cref="QuoteEmailRequest.PdfBase64"/>)
    /// so this endpoint never has to spawn Chromium just to email the same artefact the user already previewed.
    /// </summary>
    [HttpPost("email")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<QuoteEmailResponse>> EmailQuote(
        [FromBody] QuoteEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(Fail("Request body is required."));

        var to = (request.ToEmail ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(to) || !to.Contains('@', StringComparison.Ordinal))
            return BadRequest(Fail("A valid recipient email address is required."));

        if (request.Quote is null || string.IsNullOrWhiteSpace(request.Quote.QuoteRef))
            return BadRequest(Fail("Quote payload is missing or has no quote reference."));

        var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
            return BadRequest(Fail("System settings not found. Configure Postmark credentials first."));
        if (!settings.EmailEnabled)
            return BadRequest(Fail("Email is disabled in system settings."));
        if (string.IsNullOrWhiteSpace(settings.PostmarkServerToken) ||
            string.IsNullOrWhiteSpace(settings.PostmarkDefaultFromEmail))
            return BadRequest(Fail("Postmark server token and default From email must be configured in settings."));

        string token;
        try
        {
            token = _encryption.Decrypt(settings.PostmarkServerToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Quote email: failed to decrypt Postmark server token.");
            return BadRequest(Fail("Could not read stored Postmark token. Re-save the token in settings."));
        }

        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(Fail("Postmark token is empty after decrypt."));

        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? $"Quote {request.Quote.QuoteRef} — {(string.IsNullOrWhiteSpace(request.Quote.CompanyName) ? "Strong Towing" : request.Quote.CompanyName)}"
            : request.Subject!.Trim();

        var inner = QuoteEmailHtmlBuilder.BuildInnerHtml(request);
        var html = EmailLayout.WrapInFormalLayout(null, inner);
        var text = QuoteEmailHtmlBuilder.BuildPlainText(request);

        // Render the PDF server-side via Playwright/Chromium so icons + payment SVGs stay
        // vector & aligned (the previous client-side html2canvas capture rasterised them).
        var assetBase = $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase}".TrimEnd('/');
        IReadOnlyList<EmailAttachment>? attachments = null;
        try
        {
            var pdfBytes = await _quotePdfGenerator.GenerateAsync(request.Quote, assetBase, cancellationToken)
                .ConfigureAwait(false);
            var safeRef = SanitizeFileToken(request.Quote.QuoteRef);
            attachments = new[]
            {
                new EmailAttachment(
                    $"quote-{safeRef}.pdf",
                    Convert.ToBase64String(pdfBytes),
                    "application/pdf")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Quote email: server-side PDF render failed for {QuoteRef}; aborting send.",
                request.Quote.QuoteRef);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                Fail("Could not render the quote PDF on the server. Please try again in a moment."));
        }

        var stream = string.IsNullOrWhiteSpace(settings.PostmarkMessageStream) ? null : settings.PostmarkMessageStream;

        var result = await _emailSender.SendAsync(
            token,
            settings.PostmarkDefaultFromEmail!.Trim(),
            stream,
            to,
            subject,
            html,
            text,
            cancellationToken,
            attachments).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogWarning("Quote email failed: {Error}", result.ErrorMessage);
            return Ok(new QuoteEmailResponse
            {
                Success = false,
                ToEmail = to,
                ErrorMessage = result.ErrorMessage
            });
        }

        _logger.LogInformation(
            "Quote email sent for {QuoteRef} to {To} PostmarkId={Id}",
            request.Quote.QuoteRef, to, result.PostmarkMessageId);

        return Ok(new QuoteEmailResponse
        {
            Success = true,
            ToEmail = to,
            PostmarkMessageId = result.PostmarkMessageId
        });
    }

    private static QuoteEmailResponse Fail(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };

    private static string SanitizeFileToken(string token)
    {
        var chars = Path.GetInvalidFileNameChars();
        return string.Join("_", token.Split(chars, StringSplitOptions.RemoveEmptyEntries)).Trim('_');
    }
}
