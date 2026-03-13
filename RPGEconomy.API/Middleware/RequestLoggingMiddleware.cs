using System.Diagnostics;

namespace RPGEconomy.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "→ {Method} {Path}{Query}",
            request.Method,
            request.Path,
            request.QueryString);

        await _next(context);

        stopwatch.Stop();

        _logger.LogInformation(
            "← {Method} {Path} {StatusCode} ({Elapsed}ms)",
            request.Method,
            request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}
