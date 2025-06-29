using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        LogRequest(context);
        await _next(context);
    }

    private void LogRequest(HttpContext context)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        _logger.LogInformation("Incoming {Method} {Path}, TraceId={TraceId}", context.Request.Method, context.Request.Path, traceId);
    }
}
