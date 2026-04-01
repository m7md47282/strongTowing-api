namespace StrongTowing.Application.Abstractions;

/// <summary>
/// Push notifications: token registry + sending. Sending uses Firebase Admin on the server only.
/// </summary>
public interface IFcmNotificationService
{
    Task RegisterTokenAsync(string userId, string token, CancellationToken cancellationToken = default);

    Task UnregisterTokenAsync(string userId, string token, CancellationToken cancellationToken = default);

    Task<FcmSendResult> SendToUserAsync(
        string userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
