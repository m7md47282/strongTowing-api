using System.Text.RegularExpressions;

namespace StrongTowing.API.Services;

/// <summary>Normalize US-style numbers to E.164 (+1...).</summary>
public static class SmsPhoneNormalizer
{
    private static readonly Regex DigitsOnly = new(@"[^\d]", RegexOptions.Compiled);

    /// <summary>Returns null if the number cannot be normalized.</summary>
    public static string? ToE164Us(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var s = raw.Trim();
        if (s.StartsWith("+", StringComparison.Ordinal))
        {
            var digits = DigitsOnly.Replace(s, "");
            if (digits.Length is >= 10 and <= 15)
                return "+" + digits;
            return null;
        }

        var d = DigitsOnly.Replace(s, "");
        if (d.Length == 10)
            return "+1" + d;
        if (d.Length == 11 && d.StartsWith("1", StringComparison.Ordinal))
            return "+" + d;

        return null;
    }
}
