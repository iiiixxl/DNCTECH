using Authorization_Extend.Permissions.Authorization;
using Authorization_Extend.PolicyCodeAuthorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Authorization_Extend.Permissions.Authorization;

/// <summary>
/// 统一动态 Policy 提供器：策略名 = 权限编码，按需构建，无需逐个 AddPolicy。
/// </summary>
/// <remarks>
/// 路由规则（与原生 AddPolicy 共存）：
/// 1. Program 中显式 AddPolicy 注册的 → 优先使用原生策略
/// 2. PolicyCodePermissionNames 中的编码 → PermissionCodeRequirement（用户直查库）
/// 3. 其余未注册的策略名 → PermissionAuthorizationRequirement（ABP 角色授权）
/// </remarks>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return await _fallback.GetPolicyAsync(policyName);
        }

        // 原生做法：Program 中 AddPolicy 注册的优先
        var registered = await _fallback.GetPolicyAsync(policyName);
        if (registered is not null)
        {
            return registered;
        }

        // 极简方案：策略名在 PolicyCode 集合中 → 用户直查库 Handler
        IAuthorizationRequirement requirement = PolicyCodePermissionNames.All.Contains(policyName)
            ? new PermissionCodeRequirement(policyName)
            : new PermissionAuthorizationRequirement(policyName);

        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(requirement)
            .Build();

        return policy;
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
