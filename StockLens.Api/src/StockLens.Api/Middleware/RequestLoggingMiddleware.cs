using System.Diagnostics;
using Serilog.Context;

namespace StockLens.Api.Middleware;

/// <summary>
/// Middleware that assigns a unique CorrelationId to every request, enriches the Serilog
/// LogContext so every log line in the request carries the id, and emits structured
/// start / end log entries with elapsed time and HTTP status code.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ILogger<RequestLoggingMiddleware> logger)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Echo the id back so callers can correlate their own traces.
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "/";

            logger.LogInformation("Starting {Method} {Path}", method, path);

            var sw = Stopwatch.StartNew();
            try
            {
                await next(context);
                sw.Stop();

                logger.LogInformation(
                    "Completed {Method} {Path} {StatusCode} in {ElapsedMs}ms",
                    method, path, context.Response.StatusCode, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();

                logger.LogError(
                    ex,
                    "Failed {Method} {Path} after {ElapsedMs}ms",
                    method, path, sw.ElapsedMilliseconds);

                throw;
            }
        }
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds structured request logging with a per-request CorrelationId.
    /// Must be registered before <c>UseCors</c> and endpoint mappings.
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestLoggingMiddleware>();
}
