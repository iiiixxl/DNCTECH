namespace AbpDynamicProxy_Demo.DynamicProxy;

/// <summary>
/// 对应 ABP: Volo.Abp.DynamicProxy.IAbpInterceptor
/// 所有横切逻辑（授权 / UoW / 校验 / 审计）统一实现此接口。
/// </summary>
public interface IAbpInterceptor
{
    Task InterceptAsync(IAbpMethodInvocation invocation);
}
