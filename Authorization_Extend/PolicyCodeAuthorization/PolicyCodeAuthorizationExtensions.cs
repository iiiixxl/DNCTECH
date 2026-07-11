using Authorization_Extend.PolicyCodeAuthorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 注册极简（原生扩展）动态权限：IUserPermissionService + PermissionCodeHandler + 独立 PolicyProvider。
/// 本模块自包含，不依赖 Permissions（仿 ABP）模块。
/// </summary>
public static class PolicyCodeAuthorizationExtensions
{
    public static IServiceCollection AddPolicyCodeAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IUserPermissionService, InMemoryUserPermissionService>();
        services.AddScoped<IAuthorizationHandler, PermissionCodeHandler>();
        services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, PolicyCodePolicyProvider>());

        return services;
    }
}
