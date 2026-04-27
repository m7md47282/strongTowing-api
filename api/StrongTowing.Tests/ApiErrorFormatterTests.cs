using StrongTowing.API.Infrastructure;

namespace StrongTowing.Tests;

public class ApiErrorFormatterTests
{
    [Fact]
    public void PublicMessage_unknown_exception_is_generic()
    {
        var msg = ApiErrorFormatter.PublicMessage(new DivideByZeroException(), sql: null);
        Assert.Contains("unexpected", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StatusCodeFor_argument_exception_is_400()
    {
        Assert.Equal(400, ApiErrorFormatter.StatusCodeFor(new ArgumentException("x")));
    }

    [Fact]
    public void StatusCodeFor_other_exception_is_500()
    {
        Assert.Equal(500, ApiErrorFormatter.StatusCodeFor(new InvalidOperationException()));
    }

    [Fact]
    public void FindSqlException_returns_null_when_none()
    {
        Assert.Null(ApiErrorFormatter.FindSqlException(new DivideByZeroException()));
    }
}
