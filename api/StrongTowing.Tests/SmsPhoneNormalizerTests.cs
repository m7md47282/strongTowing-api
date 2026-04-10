using StrongTowing.API.Services;

namespace StrongTowing.Tests;

public class SmsPhoneNormalizerTests
{
    [Theory]
    [InlineData("5551234567", "+15551234567")]
    [InlineData("(555) 123-4567", "+15551234567")]
    [InlineData("+1 555 123 4567", "+15551234567")]
    [InlineData("+15551234567", "+15551234567")]
    public void ToE164Us_FormatsUsNumbers(string input, string expected)
    {
        Assert.Equal(expected, SmsPhoneNormalizer.ToE164Us(input));
    }

    [Fact]
    public void ToE164Us_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(SmsPhoneNormalizer.ToE164Us(null));
        Assert.Null(SmsPhoneNormalizer.ToE164Us("   "));
    }
}
