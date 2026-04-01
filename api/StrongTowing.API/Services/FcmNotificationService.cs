using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.Abstractions;
using StrongTowing.Core.Entities;
using StrongTowing.Infrastructure.Data;
using System.Text;

namespace StrongTowing.API.Services;

public class FcmNotificationService : IFcmNotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<FcmNotificationService> _logger;

    public FcmNotificationService(ApplicationDbContext db, ILogger<FcmNotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RegisterTokenAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        var normalized = token.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return;
        }

        var existing = await _db.UserFcmTokens.FirstOrDefaultAsync(t => t.Token == normalized, cancellationToken);
        var now = DateTime.UtcNow;

        if (existing != null)
        {
            existing.UserId = userId;
            existing.UpdatedAt = now;
        }
        else
        {
            _db.UserFcmTokens.Add(new UserFcmToken
            {
                UserId = userId,
                Token = normalized,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnregisterTokenAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        var normalized = token.Trim();
        await _db.UserFcmTokens
            .Where(t => t.UserId == userId && t.Token == normalized)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<FcmSendResult> SendToUserAsync(
        string userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            const string msg =
                "Firebase push is not configured on the server. Set Firebase:ServiceAccountKeyPath in appsettings to a valid service account JSON file and restart the API.";
            _logger.LogWarning("Firebase Admin is not initialized; cannot send push notifications.");
            return FcmSendResult.Failed(msg);
        }

        var tokens = await _db.UserFcmTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            _logger.LogWarning("No FCM tokens registered for user {UserId}; push cannot be sent.", userId);
            return FcmSendResult.Failed(
                "This driver has no registered push devices. They must open the driver app in the browser, sign in, and allow notifications so a device token can be saved.");
        }

        try
        {
            var totalSuccess = 0;
            string? lastError = null;

            const int batchSize = 500;
            for (var i = 0; i < tokens.Count; i += batchSize)
            {
                var batch = tokens.Skip(i).Take(batchSize).ToList();
                var message = new MulticastMessage
                {
                    Tokens = batch,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    }
                };

                if (data is { Count: > 0 })
                {
                    message.Data = data.ToDictionary(kv => kv.Key, kv => kv.Value);
                }

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken);
                totalSuccess += response.SuccessCount;

                if (response.FailureCount > 0)
                {
                    await PruneInvalidTokensAsync(response, batch, cancellationToken);
                    lastError = DescribeMulticastFailures(response, batch);
                    if (response.SuccessCount == 0 && !string.IsNullOrEmpty(lastError))
                    {
                        _logger.LogWarning(
                            "FCM multicast had failures for user {UserId}: {Detail}",
                            userId,
                            lastError);
                    }
                }
            }

            if (totalSuccess == 0)
            {
                var detail = string.IsNullOrWhiteSpace(lastError)
                    ? "Firebase rejected all delivery attempts for this user's tokens."
                    : lastError;
                return FcmSendResult.Failed($"Push notification could not be delivered: {detail}");
            }

            return FcmSendResult.Ok();
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "FCM FirebaseMessagingException for user {UserId}", userId);
            return FcmSendResult.Failed($"Firebase messaging error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM send failed for user {UserId}", userId);
            return FcmSendResult.Failed($"Push notification failed: {ex.Message}");
        }
    }

    private static string DescribeMulticastFailures(BatchResponse response, IReadOnlyList<string> batchTokens)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < response.Responses.Count; i++)
        {
            var r = response.Responses[i];
            if (r.IsSuccess)
            {
                continue;
            }

            var tokenPreview = batchTokens.Count > i && batchTokens[i].Length > 12
                ? batchTokens[i][..8] + "…"
                : "(token)";
            var err = r.Exception?.Message ?? r.Exception?.MessagingErrorCode?.ToString() ?? "unknown error";
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(tokenPreview).Append(": ").Append(err);
            if (sb.Length > 500)
            {
                break;
            }
        }

        return sb.Length > 0 ? sb.ToString() : string.Empty;
    }

    private async Task PruneInvalidTokensAsync(
        BatchResponse response,
        IReadOnlyList<string> batchTokens,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < response.Responses.Count; i++)
        {
            var r = response.Responses[i];
            if (r.IsSuccess || r.Exception?.MessagingErrorCode == null)
            {
                continue;
            }

            var code = r.Exception.MessagingErrorCode;
            if (code is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
            {
                var token = batchTokens[i];
                await _db.UserFcmTokens.Where(t => t.Token == token).ExecuteDeleteAsync(cancellationToken);
            }
        }
    }

    /// <summary>Call once at startup when <see cref="FirebaseOptions.ServiceAccountKeyPath"/> is set.</summary>
    public static void TryInitializeFirebase(string? serviceAccountKeyPath, ILogger logger)
    {
        if (FirebaseApp.DefaultInstance != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(serviceAccountKeyPath) || !File.Exists(serviceAccountKeyPath))
        {
            logger.LogWarning(
                "Firebase service account file not found at {Path}. Push sending is disabled until configured.",
                serviceAccountKeyPath ?? "(null)");
            return;
        }

        try
        {
            using var stream = File.OpenRead(serviceAccountKeyPath);
#pragma warning disable CS0618 // Google.Apis.Auth: use CredentialFactory when upgrading
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromStream(stream)
            });
#pragma warning restore CS0618
            logger.LogInformation("Firebase Admin initialized for FCM.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Firebase Admin.");
        }
    }
}
