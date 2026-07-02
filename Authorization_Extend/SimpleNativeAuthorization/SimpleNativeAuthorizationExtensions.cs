using Authorization_Extend.SimpleNativeAuthorization;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 原生授权扩展：集中注册 Policy，配合 [Authorize(Policy = "...")] 使用。
/// </summary>
public static class SimpleNativeAuthorizationExtensions
{
    /// <summary>
    /// 注册原生图书 Policy（基于 Role，需在 Program 中显式 AddPolicy）。
    /// </summary>
    public static IServiceCollection AddNativeBookPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // 原生做法：每个操作一个 Policy，Policy 内绑定 Role
            options.AddPolicy(NativeBookPolicyNames.View, policy =>
                policy.RequireRole("Admin", "User"));

            options.AddPolicy(NativeBookPolicyNames.Create, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(NativeBookPolicyNames.Update, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(NativeBookPolicyNames.Delete, policy =>
                policy.RequireRole("Admin"));
        });

        return services;
    }
}
