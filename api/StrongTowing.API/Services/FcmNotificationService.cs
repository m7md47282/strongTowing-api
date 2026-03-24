using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.Abstractions;
using StrongTowing.Core.Entities;
using StrongTowing.Infrastructure.Data;

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

    public async Task SendToUserAsync(
        string userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("Firebase Admin is not initialized; cannot send push notifications.");
            return;
        }

        var tokens = await _db.UserFcmTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            return;
        }

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

            if (response.FailureCount > 0)
            {
                await PruneInvalidTokensAsync(response, batch, cancellationToken);
            }
        }
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
