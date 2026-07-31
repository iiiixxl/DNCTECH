using AbpDynamicProxy_Demo.DynamicProxy;

namespace AbpDynamicProxy_Demo.Authorization;

/// <summary>
/// 对应 ABP: AuthorizationInterceptor
/// 先鉴权，再 ProceedAsync 进入下一个拦截器 / 真实方法。
/// </summary>
public class AuthorizationInterceptor : AbpInterceptor
{
    private readonly IMethodInvocationAuthorizationService _authorizationService;
    private readonly ILogger<AuthorizationInterceptor> _logger;

    public AuthorizationInterceptor(
        IMethodInvocationAuthorizationService authorizationService,
        ILogger<AuthorizationInterceptor> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public override async Task InterceptAsync(IAbpMethodInvocation invocation)
    {
        _logger.LogInformation(
            "[AuthorizationInterceptor] checking {Type}.{Method}",
            invocation.Method.DeclaringType?.Name,
            invocation.Method.Name);

        await _authorizationService.CheckAsync(invocation.Method);

        await invocation.ProceedAsync();
    }
}
