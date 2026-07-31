using Castle.DynamicProxy;

namespace AbpDynamicProxy_Demo.DynamicProxy;

/// <summary>
/// 对应 ABP: AbpAsyncDeterminationInterceptor&lt;TInterceptor&gt;
/// Autofac InterceptedBy 实际注册的是这个 Castle 拦截器类型。
/// </summary>
public class AbpAsyncDeterminationInterceptor<TInterceptor> : AsyncDeterminationInterceptor
    where TInterceptor : IAbpInterceptor
{
    public AbpAsyncDeterminationInterceptor(TInterceptor abpInterceptor)
        : base(new CastleAsyncAbpInterceptorAdapter<TInterceptor>(abpInterceptor))
    {
    }
}
