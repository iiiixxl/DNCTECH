using Authorization_Extend.FieldLevelAuthorization;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 注册「字段级动态授权」：员工数据源 + 组装服务 + 字段 Handler。
/// 本模块完全自包含，不替换 IAuthorizationPolicyProvider，
/// 与仿 ABP / 极简 PolicyCode / 资源授权等方案互不冲突，可同时启用。
/// </summary>
public static class FieldLevelAuthorizationExtensions
{
    public static IServiceCollection AddFieldLevelAuthorization(this IServiceCollection services)
    {
        // 让本模块可单独启用；重复调用会合并，不会覆盖已有授权配置。
        services.AddAuthorization();

        services.AddSingleton<IEmployeeStore, InMemoryEmployeeStore>();
        services.AddScoped<EmployeeService>();

        // 先做行级资源授权，阻止跨用户读取档案。
        services.AddSingleton<IAuthorizationHandler, EmployeeProfileAccessHandler>();

        // 资源型 Handler：AuthorizationHandler&lt;FieldAccessRequirement, EmployeeDto&gt;
        // 框架在 AuthorizeAsync(user, dto, requirement) 时按类型自动路由到这里
        services.AddSingleton<IAuthorizationHandler, FieldAccessHandler>();

        return services;
    }
}
