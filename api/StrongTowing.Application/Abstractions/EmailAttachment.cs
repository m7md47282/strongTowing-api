namespace StrongTowing.Application.Abstractions;

/// <summary>
/// File attachment for transactional email send (e.g. PDF quote).
/// </summary>
/// <param name="Name">File name displayed to the recipient (e.g. <c>quote-Q12345.pdf</c>).</param>
/// <param name="ContentBase64">Base64-encoded payload (no data URI prefix).</param>
/// <param name="ContentType">MIME type, e.g. <c>application/pdf</c>.</param>
public sealed record EmailAttachment(string Name, string ContentBase64, string ContentType);
