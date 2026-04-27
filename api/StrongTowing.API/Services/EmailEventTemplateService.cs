using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StrongTowing.API.Options;
using StrongTowing.Application.Abstractions;
using StrongTowing.Application.EmailTemplates;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Services;

public sealed class EmailEventTemplateService : IEmailEventTemplateService
{
    private readonly ApplicationDbContext _db;
    private readonly EmailBrandingOptions _emailBranding;

    public EmailEventTemplateService(ApplicationDbContext db, IOptions<EmailBrandingOptions> emailBranding)
    {
        _db = db;
        _emailBranding = emailBranding.Value;
    }

    public async Task<EmailRenderResult> RenderAsync(
        string eventKey,
        IReadOnlyDictionary<string, string> mergeFields,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventKey) ||
            !EmailEventKeys.All.Any(x => string.Equals(x, eventKey, StringComparison.Ordinal)))
            throw new ArgumentException("Unknown email event key.", nameof(eventKey));

        var def = EmailTemplateDefinitions.ByKey(eventKey);
        if (def == null)
            throw new InvalidOperationException($"No template definition for event: {eventKey}.");

        var logo = _emailBranding.ResolveLogoAbsoluteUrl();

        var mergeWithBranding = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in mergeFields)
            mergeWithBranding[kv.Key] = kv.Value;
        // Reserved: absolute URL for the email header logo; also available as {{LogoUrl}} in inner HTML if needed.
        mergeWithBranding["LogoUrl"] = logo ?? string.Empty;

        var row = await _db.SystemEmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.EventKey == eventKey, cancellationToken);

        var subjectSrc = (row is { Subject: not null } s && s.Subject.Length > 0) ? s.Subject : def.DefaultSubject;
        var innerSrc = (row is { HtmlBody: not null } h && h.HtmlBody.Length > 0) ? h.HtmlBody : def.DefaultInnerHtml;

        var mergedSubject = EmailTemplateMerge.Apply(subjectSrc, mergeWithBranding);
        var mergedInner = EmailTemplateMerge.Apply(innerSrc, mergeWithBranding);
        var html = EmailLayout.WrapInFormalLayout(logo, mergedInner);

        string plain;
        if (row is { TextBody: not null } t && t.TextBody.Trim().Length > 0)
        {
            plain = EmailTemplateMerge.Apply(t.TextBody, mergeWithBranding);
        }
        else
        {
            plain = EmailLayout.HtmlToPlainText(html);
        }

        return new EmailRenderResult(mergedSubject, html, plain);
    }
}
