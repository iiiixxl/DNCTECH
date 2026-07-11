using Authorization_Extend.ClaimsTransformation;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 注册「声明转换」：画像数据源 + IClaimsTransformation + 一条基于权限声明的策略。
/// 本模块自包含，不替换 IAuthorizationPolicyProvider，与其它鉴权方案可同时启用。
/// </summary>
public static class ClaimsTransformationExtensions
{
    public static IServiceCollection AddClaimsTransformation(this IServiceCollection services)
    {
        services.AddSingleton<IUserProfileService, InMemoryUserProfileService>();

        // 同时登记具体类型：临时权限篇的 CompositeClaimsTransformation 需要按类型解析本转换器，
        // 才能与 TempPermissionClaimsTransformer 串联，避免两个 IClaimsTransformation 互相覆盖。
        services.AddTransient<PermissionClaimsTransformer>();

        // 官方建议用 Transient/Scoped：转换器每次认证都会被调用，别用 Singleton 缓存请求态
        services.AddTransient<IClaimsTransformation, PermissionClaimsTransformer>();

        // 用转换补出来的「permission」声明做授权，原生 AddPolicy 登记即可
        services.AddAuthorization(options =>
        {
            options.AddPolicy(ClaimsPermissionPolicyNames.FinanceApprove, policy =>
                policy.RequireClaim(EnrichedClaimTypes.Permission, "finance.approve"));
        });

        return services;
    }
}
