using Authorization_Extend.PolicyCodeAuthorization;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 注册极简动态权限：IUserPermissionService + PermissionCodeHandler。
/// PolicyProvider 由 Permissions 模块的统一 Provider 负责路由。
/// </summary>
public static class PolicyCodeAuthorizationExtensions
{
    public static IServiceCollection AddPolicyCodeAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IUserPermissionService, InMemoryUserPermissionService>();
        services.AddScoped<IAuthorizationHandler, PermissionCodeHandler>();

        return services;
    }
}
