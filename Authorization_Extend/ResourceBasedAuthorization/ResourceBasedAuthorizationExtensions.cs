using Authorization_Extend.ResourceBasedAuthorization;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 注册「基于资源的动态授权」：订单数据源 + 两个资源 Handler + 两条命名策略。
/// 本模块完全自包含，且不替换 IAuthorizationPolicyProvider，
/// 因此与仿 ABP / 极简 PolicyCode 两套动态权限互不冲突，可同时启用。
/// </summary>
public static class ResourceBasedAuthorizationExtensions
{
    public static IServiceCollection AddResourceBasedAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IOrderStore, InMemoryOrderStore>();

        // 资源型 Handler：AuthorizationHandler<TRequirement, TResource>，框架按类型自动路由
        services.AddSingleton<IAuthorizationHandler, SameTenantAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, OrderOwnerAuthorizationHandler>();

        // 用原生 AddPolicy 登记两条策略。这是「命名策略 + 传资源」用法的前提，
        // 也保证了另外两套动态 Provider 的 fallback 能优先命中这里注册的策略。
        services.AddAuthorization(options =>
        {
            options.AddPolicy(ResourceAuthorizationPolicyNames.SameTenant, policy =>
                policy.Requirements.Add(new SameTenantRequirement()));

            options.AddPolicy(ResourceAuthorizationPolicyNames.OrderOwner, policy =>
                policy.Requirements.Add(new OrderOwnerRequirement()));
        });

        return services;
    }
}
