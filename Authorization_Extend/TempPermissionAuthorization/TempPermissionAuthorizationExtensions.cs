using Authorization_Extend.ClaimsTransformation;
using Authorization_Extend.TempPermissionAuthorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 注册「临时权限授予」：授权库 + 声明转换 + Handler + 策略。
/// 不替换 IAuthorizationPolicyProvider，与其它鉴权方案可共存。
/// </summary>
public static class TempPermissionAuthorizationExtensions
{
    public static IServiceCollection AddTempPermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<ITempPermissionStore, InMemoryTempPermissionStore>();

        // 转换器本身用 Transient；真正挂到 IClaimsTransformation 的是下面的 Composite
        services.AddTransient<TempPermissionClaimsTransformer>();

        // ASP.NET Core 只会解析「一个」IClaimsTransformation；用 Composite 串联，避免后注册覆盖前者。
        // PermissionClaimsTransformer 由 AddClaimsTransformation 登记为具体类型；未启用时 GetService 为 null。
        services.Replace(ServiceDescriptor.Transient<IClaimsTransformation>(sp =>
        {
            var chain = new List<IClaimsTransformation>();

            var profileTransformer = sp.GetService<PermissionClaimsTransformer>();
            if (profileTransformer is not null)
            {
                chain.Add(profileTransformer);
            }

            chain.Add(sp.GetRequiredService<TempPermissionClaimsTransformer>());
            return chain.Count == 1
                ? chain[0]
                : new CompositeClaimsTransformation(chain);
        }));

        services.AddSingleton<IAuthorizationHandler, TempPermissionHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(TempPermissionNames.ExpenseApprovePolicy, policy =>
                policy.Requirements.Add(
                    new TempPermissionRequirement(TempPermissionNames.ExpenseApprove)));
        });

        return services;
    }
}
