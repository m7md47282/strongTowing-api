using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StrongTowing.Application.Abstractions;

namespace StrongTowing.API.Services;

public sealed class PostmarkEmailSender : IEmailSender
{
    private readonly HttpClient _http;

    public PostmarkEmailSender(HttpClient http)
    {
        _http = http;
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri("https://api.postmarkapp.com/");
    }

    public async Task<EmailSendResult> SendAsync(
        string serverToken,
        string fromEmail,
        string? messageStream,
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody,
        CancellationToken cancellationToken = default,
        IReadOnlyList<EmailAttachment>? attachments = null)
    {
        if (string.IsNullOrWhiteSpace(serverToken))
            return new EmailSendResult(false, "Postmark server token is not configured.");
        if (string.IsNullOrWhiteSpace(fromEmail))
            return new EmailSendResult(false, "From email is not configured.");
        if (string.IsNullOrWhiteSpace(toEmail))
            return new EmailSendResult(false, "Recipient email is required.");
        if (string.IsNullOrWhiteSpace(htmlBody) && string.IsNullOrWhiteSpace(textBody))
            return new EmailSendResult(false, "Email body is required.");

        var stream = string.IsNullOrWhiteSpace(messageStream) ? "outbound" : messageStream.Trim();
        var payload = new PostmarkEmailPayload
        {
            From = fromEmail.Trim(),
            To = toEmail.Trim(),
            Subject = subject.Trim(),
            HtmlBody = string.IsNullOrWhiteSpace(htmlBody) ? null : htmlBody,
            TextBody = string.IsNullOrWhiteSpace(textBody) ? null : textBody,
            MessageStream = stream
        };

        if (payload.HtmlBody == null && payload.TextBody != null)
            payload.HtmlBody = $"<pre style=\"font-family:sans-serif\">{System.Net.WebUtility.HtmlEncode(payload.TextBody)}</pre>";

        if (attachments is { Count: > 0 })
        {
            var list = new List<PostmarkAttachmentPayload>(attachments.Count);
            foreach (var a in attachments)
            {
                if (a is null || string.IsNullOrWhiteSpace(a.Name) || string.IsNullOrWhiteSpace(a.ContentBase64))
                    continue;
                list.Add(new PostmarkAttachmentPayload
                {
                    Name = a.Name,
                    Content = a.ContentBase64,
                    ContentType = string.IsNullOrWhiteSpace(a.ContentType) ? "application/octet-stream" : a.ContentType
                });
            }
            if (list.Count > 0)
                payload.Attachments = list;
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, "email");
        req.Headers.TryAddWithoutValidation("X-Postmark-Server-Token", serverToken.Trim());
        req.Content = new StringContent(JsonSerializer.Serialize(payload, PostmarkJsonOptions), Encoding.UTF8, "application/json");

        try
        {
            var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                var ok = JsonSerializer.Deserialize<PostmarkSuccessResponse>(json, PostmarkJsonOptions);
                return new EmailSendResult(true, null, ok?.MessageID);
            }

            var err = JsonSerializer.Deserialize<PostmarkErrorResponse>(json, PostmarkJsonOptions);
            var msg = err?.Message ?? resp.ReasonPhrase ?? "Postmark request failed.";
            return new EmailSendResult(false, msg);
        }
        catch (Exception ex)
        {
            return new EmailSendResult(false, ex.Message);
        }
    }

    /// <summary>Postmark expects PascalCase property names in JSON (From, To, HtmlBody, …).</summary>
    private static readonly JsonSerializerOptions PostmarkJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null
    };

    private sealed class PostmarkEmailPayload
    {
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string Subject { get; set; } = "";
        public string? HtmlBody { get; set; }
        public string? TextBody { get; set; }
        public string MessageStream { get; set; } = "outbound";
        public List<PostmarkAttachmentPayload>? Attachments { get; set; }
    }

    private sealed class PostmarkAttachmentPayload
    {
        public string Name { get; set; } = "";
        public string Content { get; set; } = "";
        public string ContentType { get; set; } = "application/octet-stream";
    }

    private sealed class PostmarkSuccessResponse
    {
        [JsonPropertyName("MessageID")]
        public string? MessageID { get; set; }
    }

    private sealed class PostmarkErrorResponse
    {
        [JsonPropertyName("Message")]
        public string? Message { get; set; }
    }
}
