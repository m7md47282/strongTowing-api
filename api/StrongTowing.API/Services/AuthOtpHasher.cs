using System.Security.Cryptography;
using System.Text;

namespace StrongTowing.API.Services;

public static class AuthOtpHasher
{
    public static string HashOtp(string normalizedEmail, string otpCode, string pepper)
    {
        if (string.IsNullOrWhiteSpace(pepper))
            throw new InvalidOperationException("Auth OTP pepper is not configured.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
        var payload = Encoding.UTF8.GetBytes($"{normalizedEmail.Trim().ToUpperInvariant()}:{otpCode.Trim()}");
        return Convert.ToBase64String(hmac.ComputeHash(payload));
    }

    public static bool Verify(string normalizedEmail, string otpCode, string pepper, string storedHash)
    {
        try
        {
            var a = Convert.FromBase64String(HashOtp(normalizedEmail, otpCode, pepper));
            var b = Convert.FromBase64String(storedHash);
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
        catch
        {
            return false;
        }
    }
}
