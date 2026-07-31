namespace AbpDynamicProxy_Demo.AdapterConcept.Foreign;

/// <summary>
/// 模拟第三方要求你实现的拦截器接口（类似 Castle 的 IInterceptor）。
/// 注意：方法名、参数类型都是第三方定的。
/// </summary>
public interface IForeignInterceptor
{
    void Intercept(ForeignInvocation invocation);
}
