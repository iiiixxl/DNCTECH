using AbpDynamicProxy_Demo.DynamicProxy;
using Autofac;
using Autofac.Extras.DynamicProxy;

namespace AbpDynamicProxy_Demo.DependencyInjection;

/// <summary>
/// 对应 ABP Autofac: InvokeRegistrationActions + AddInterceptors
/// 注册服务时跑 OnRegistered 钩子，若有拦截器则 EnableInterfaceInterceptors + InterceptedBy。
/// </summary>
public static class AutofacAbpRegistrationExtensions
{
    public static void RegisterAbpStyleService<TService, TImplementation>(
        this ContainerBuilder builder,
        ServiceRegistrationActionList registrationActions)
        where TService : class
        where TImplementation : class, TService
    {
        var context = new OnServiceRegistredContext(typeof(TService), typeof(TImplementation));
        foreach (var action in registrationActions)
        {
            action(context);
        }

        var registration = builder
            .RegisterType<TImplementation>()
            .As<TService>()
            .InstancePerLifetimeScope();

        if (context.Interceptors.Count == 0)
        {
            return;
        }

        // 对应 ABP: EnableInterfaceInterceptors / EnableClassInterceptors
        registration = registration.EnableInterfaceInterceptors();

        foreach (var interceptorType in context.Interceptors)
        {
            // 对应 ABP: InterceptedBy(typeof(AbpAsyncDeterminationInterceptor<>).MakeGenericType(interceptor))
            var castleInterceptorType = typeof(AbpAsyncDeterminationInterceptor<>)
                .MakeGenericType(interceptorType);

            registration.InterceptedBy(castleInterceptorType);
        }
    }
}
