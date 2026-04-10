using StrongTowing.Tests.Fakes;

namespace StrongTowing.Tests;

public class RecordingSmsSenderTests
{
    [Fact]
    public async Task SendAsync_RecordsToAndBody()
    {
        var s = new RecordingSmsSender();
        await s.SendAsync("ACx", "token", "+1", null, "+15551234567", "Hello", default);

        var call = Assert.Single(s.Calls);
        Assert.Equal("+15551234567", call.ToE164);
        Assert.Equal("Hello", call.Body);
    }
}
