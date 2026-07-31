using System.Reflection;
using AbpDynamicProxy_Demo.DependencyInjection;

namespace AbpDynamicProxy_Demo.Authorization;

/// <summary>
/// 对应 ABP: AuthorizationInterceptorRegistrar
/// 类上或任意方法上有 PermissionAuthorize → 给该类型挂 AuthorizationInterceptor。
/// </summary>
public static class AuthorizationInterceptorRegistrar
{
    public static void RegisterIfNeeded(OnServiceRegistredContext context)
    {
        if (ShouldIntercept(context.ImplementationType))
        {
            context.TryAddInterceptor<AuthorizationInterceptor>();
        }
    }

    private static bool ShouldIntercept(Type type)
    {
        if (type.IsDefined(typeof(PermissionAuthorizeAttribute), true))
        {
            return true;
        }

        return type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(m => m.IsDefined(typeof(PermissionAuthorizeAttribute), true));
    }
}
