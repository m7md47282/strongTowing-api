using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;

namespace StrongTowing.API.Infrastructure;

/// <summary>Consistent JSON bodies for API failures (especially SQL / migrations).</summary>
public static class ApiErrorFormatter
{
    /// <summary>HTTP status to use for this exception (usually 500).</summary>
    public static int StatusCodeFor(Exception ex) =>
        ex is ArgumentException or ArgumentNullException ? 400 : 500;

    /// <summary>Structured payload for JSON responses and exception middleware.</summary>
    public static object Build(HttpContext http, IWebHostEnvironment env, Exception ex, string? operation = null)
    {
        var sql = FindSqlException(ex);
        var dev = env.IsDevelopment();
        return new
        {
            error = StatusCodeFor(ex) == 400 ? "Bad Request" : "Internal Server Error",
            message = PublicMessage(ex, sql),
            operation,
            sqlNumber = sql?.Number,
            sqlMessage = sql?.Message,
            detail = dev ? ex.ToString() : null,
            traceId = http.TraceIdentifier
        };
    }

    public static string PublicMessage(Exception ex, SqlException? sql)
    {
        if (sql != null)
        {
            // Invalid object name — table/view missing (migrations not applied).
            if (sql.Number == 208 || sql.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
            {
                return "The database schema is missing required objects (for example email template tables). Apply the latest EF Core migrations on this server, then retry.";
            }

            return $"Database error ({sql.Number}): {sql.Message}";
        }

        return ex switch
        {
            ArgumentNullException ane => ane.Message,
            ArgumentException ae => ae.Message,
            InvalidOperationException ioe => ioe.Message,
            _ => "An unexpected error occurred. Use traceId when contacting support."
        };
    }

    public static SqlException? FindSqlException(Exception? exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is SqlException sqlException)
                return sqlException;
            current = current.InnerException;
        }

        return null;
    }
}
