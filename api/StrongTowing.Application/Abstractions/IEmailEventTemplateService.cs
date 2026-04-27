namespace StrongTowing.Application.Abstractions;

public sealed record EmailRenderResult(string Subject, string HtmlBody, string PlainTextBody);

public interface IEmailEventTemplateService
{
    /// <summary>
    /// Resolves subject + full HTML (with formal layout) + plain text for an event.
    /// <paramref name="mergeFields"/> keys are without braces, e.g. "JobId" -> replaces {{JobId}}.
    /// </summary>
    Task<EmailRenderResult> RenderAsync(
        string eventKey,
        IReadOnlyDictionary<string, string> mergeFields,
        CancellationToken cancellationToken = default);
}
