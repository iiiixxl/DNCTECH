using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Authorization_Extend.PolicyCodeAuthorization;

/// <summary>
/// 极简（原生扩展）动态 Policy 提供器：策略名 = 权限编码，按需现场构建，无需 AddPolicy。
/// 完全独立于 Permissions（仿 ABP）模块，仅服务于 PolicyCode 方案。
/// </summary>
/// <remarks>
/// 路由规则：
/// 1. Program 中显式 AddPolicy 注册的 → 优先使用原生策略
/// 2. 命中 PolicyCodePermissionNames 集合 → PermissionCodeRequirement（用户直查库）
/// 3. 其余策略名 → 交回默认 Provider，避免误吞其它方案的策略
/// </remarks>
public class PolicyCodePolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PolicyCodePolicyProvider(IOptions<AuthorizationOptions> options)
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
        if (PolicyCodePermissionNames.All.Contains(policyName))
        {
            return new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionCodeRequirement(policyName))
                .Build();
        }

        return null;
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
