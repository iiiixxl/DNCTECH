using System.Diagnostics;
using System.Net.Http.Headers;

namespace Refit_Demo.RefitDemo.Handlers;

/// <summary>
/// 把当前入站请求的 Bearer Token 转发到 Refit 出站请求（微服务常见模式）。
/// 也可改成读独立 TokenStore；本 Demo 用「透传」即可演示横切鉴权。
/// </summary>
public class DemoAuthHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DemoAuthHeaderHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var incoming = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(incoming)
            && AuthenticationHeaderValue.TryParse(incoming, out var header)
            && header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(header.Parameter))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", header.Parameter);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// 出站可观测：补 TraceId，记录耗时。
/// </summary>
public class DemoTelemetryHandler : DelegatingHandler
{
    private readonly ILogger<DemoTelemetryHandler> _logger;

    public DemoTelemetryHandler(ILogger<DemoTelemetryHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        if (!request.Headers.Contains("X-Trace-Id"))
        {
            request.Headers.TryAddWithoutValidation("X-Trace-Id", traceId);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            _logger.LogInformation(
                "Refit 出站 {Method} {Uri} → {StatusCode} ({ElapsedMs}ms) TraceId={TraceId}",
                request.Method,
                request.RequestUri,
                (int)response.StatusCode,
                sw.ElapsedMilliseconds,
                traceId);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "Refit 出站失败 {Method} {Uri} ({ElapsedMs}ms) TraceId={TraceId}",
                request.Method,
                request.RequestUri,
                sw.ElapsedMilliseconds,
                traceId);
            throw;
        }
    }
}
