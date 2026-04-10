namespace StrongTowing.API.Options;

public class AuthOtpOptions
{
    public const string SectionName = "AuthOtp";

    /// <summary>HMAC secret for OTP hashing (use a long random string in production).</summary>
    public string Pepper { get; set; } = "StrongTowing-AuthOtp-Pepper-ChangeInProduction-Min32Chars!!";

    public int ExpiryMinutes { get; set; } = 10;

    public int CodeLength { get; set; } = 6;

    public int MaxFailedAttempts { get; set; } = 5;
}
