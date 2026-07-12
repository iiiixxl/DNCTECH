using Authorization_Extend.ResourceBasedAuthorization;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 注册「基于资源的动态授权」：数据源 + 两个通用资源 Handler + 三条命名策略（含组合策略）。
/// 本模块完全自包含，且不替换 IAuthorizationPolicyProvider，
/// 因此与仿 ABP / 极简 PolicyCode 两套动态权限互不冲突，可同时启用。
/// </summary>
public static class ResourceBasedAuthorizationExtensions
{
    public static IServiceCollection AddResourceBasedAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IOrderStore, InMemoryOrderStore>();
        services.AddSingleton<IContractStore, InMemoryContractStore>();

        // 资源型 Handler：AuthorizationHandler<TRequirement, TResource>，框架按类型自动路由
        services.AddSingleton<IAuthorizationHandler, SameTenantAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, OwnedResourceAuthorizationHandler>();

        // 用原生 AddPolicy 登记策略。一条策略可挂多个 Requirement（AND 关系），
        // 框架会分别路由到 SameTenantAuthorizationHandler / OwnedResourceAuthorizationHandler，无需再写组合 Handler。
        services.AddAuthorization(options =>
        {
            options.AddPolicy(ResourceAuthorizationPolicyNames.SameTenant, policy =>
                policy.Requirements.Add(new SameTenantRequirement()));

            options.AddPolicy(ResourceAuthorizationPolicyNames.Owner, policy =>
                policy.Requirements.Add(new OwnedResourceRequirement()));

            // 同时校验租户 + 归属人：两个 Requirement 都要 Succeed 才算通过
            options.AddPolicy(ResourceAuthorizationPolicyNames.OwnerInTenant, policy =>
            {
                policy.Requirements.Add(new SameTenantRequirement());
                policy.Requirements.Add(new OwnedResourceRequirement());
            });
        });

        return services;
    }
}
