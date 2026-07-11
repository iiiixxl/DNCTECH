using Authorization_Extend.AuthResultHandler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 注册「自定义授权结果处理器」：用 CustomAuthResultHandler 替换框架默认的
/// IAuthorizationMiddlewareResultHandler，统一改写拒绝时的响应。
/// 本模块自包含，不替换 IAuthorizationPolicyProvider，与其它鉴权方案可同时启用。
/// </summary>
public static class AuthResultHandlerExtensions
{
    public static IServiceCollection AddCustomAuthResultHandler(this IServiceCollection services)
    {
        // 该处理器容器里全局唯一，用 Replace 换掉默认的 AuthorizationMiddlewareResultHandler
        services.Replace(
            ServiceDescriptor.Singleton<IAuthorizationMiddlewareResultHandler, CustomAuthResultHandler>());

        return services;
    }
}
