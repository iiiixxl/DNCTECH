namespace AbpDynamicProxy_Demo.AdapterConcept.App;

/// <summary>
/// 我们自己定义的拦截器抽象（类似 ABP 的 IAbpInterceptor）。
/// </summary>
public interface IAppInterceptor
{
    Task InterceptAsync(IAppInvocation invocation);
}
