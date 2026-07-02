using Authorization_Extend.Permissions;
using Authorization_Extend.Permissions.Authorization;
using Authorization_Extend.Permissions.Providers;
using Authorization_Extend.Permissions.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 注册动态权限框架：DefinitionProvider + GrantStore + 统一 Handler + 动态 PolicyProvider。
/// </summary>
public static class DynamicPermissionServiceExtensions
{
    public static IServiceCollection AddDynamicPermissions(this IServiceCollection services)
    {
        services.AddSingleton<IReportStore, InMemoryReportStore>();
        services.AddSingleton<IPermissionGrantStore, InMemoryPermissionGrantStore>();

        services.AddSingleton<IPermissionDefinitionProvider, BookPermissionDefinitionProvider>();
        services.AddSingleton<IPermissionDefinitionProvider, ReportPermissionDefinitionProvider>();
        services.AddSingleton<IPermissionDefinitionManager, PermissionDefinitionManager>();

        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddSingleton<IDynamicPermissionRegistrar, DynamicPermissionRegistrar>();

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>());

        return services;
    }
}
