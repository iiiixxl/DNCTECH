namespace AbpDynamicProxy_Demo.DynamicProxy;

/// <summary>
/// 对应 ABP: Volo.Abp.DynamicProxy.AbpInterceptor
/// </summary>
public abstract class AbpInterceptor : IAbpInterceptor
{
    public abstract Task InterceptAsync(IAbpMethodInvocation invocation);
}
