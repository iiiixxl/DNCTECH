using AbpDynamicProxy_Demo.DynamicProxy;

namespace AbpDynamicProxy_Demo.Authorization;

/// <summary>
/// 演示管道里的第二个拦截器（对应 ABP 里 UoW / Validation 等同层概念）。
/// </summary>
public class LoggingInterceptor : AbpInterceptor
{
    private readonly ILogger<LoggingInterceptor> _logger;

    public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task InterceptAsync(IAbpMethodInvocation invocation)
    {
        _logger.LogInformation(
            "[LoggingInterceptor] ENTER {Type}.{Method}",
            invocation.Method.DeclaringType?.Name,
            invocation.Method.Name);

        await invocation.ProceedAsync();

        _logger.LogInformation(
            "[LoggingInterceptor] LEAVE {Type}.{Method}",
            invocation.Method.DeclaringType?.Name,
            invocation.Method.Name);
    }
}

public static class LoggingInterceptorRegistrar
{
    public static void RegisterIfNeeded(DependencyInjection.OnServiceRegistredContext context)
    {
        // 演示：凡是挂了授权拦截器的服务，也挂日志拦截器，形成管道
        if (context.Interceptors.Contains(typeof(AuthorizationInterceptor)))
        {
            context.TryAddInterceptor<LoggingInterceptor>();
        }
    }
}
