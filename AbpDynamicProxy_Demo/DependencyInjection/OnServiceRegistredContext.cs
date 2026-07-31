using AbpDynamicProxy_Demo.DynamicProxy;

namespace AbpDynamicProxy_Demo.DependencyInjection;

/// <summary>
/// 对应 ABP: OnServiceRegistredContext
/// 每个服务注册时，各 Registrar 往 Interceptors 列表里追加拦截器类型。
/// </summary>
public class OnServiceRegistredContext
{
    public Type ServiceType { get; }

    public Type ImplementationType { get; }

    public List<Type> Interceptors { get; } = new();

    public OnServiceRegistredContext(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    public void TryAddInterceptor<TInterceptor>()
        where TInterceptor : IAbpInterceptor
    {
        if (!Interceptors.Contains(typeof(TInterceptor)))
        {
            Interceptors.Add(typeof(TInterceptor));
        }
    }
}
