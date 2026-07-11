using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.TempPermissionAuthorization;

/// <summary>
/// 临时权限 Handler：校验用户是否持有指定临时权限，且对应的 TempValidUntil 尚未过期。
/// 过期则不 Succeed → 框架返回 403，实现「时效一到自动失效」。
/// </summary>
/// <remarks>
/// 双保险设计：
/// 1. 声明转换器会先摘掉过期 Claim，正常路径下过期权限根本进不了授权；
/// 2. Handler 再读一遍 TempValidUntil，防止有人绕过转换器、或 Claim 被直接塞进 Token。
///
/// Claim 约定（成对写入，避免多条授权时对不上号）：
/// - TempPermission = "expense.approve"
/// - TempValidUntil = "expense.approve|2026-07-11T06:00:00.0000000+00:00"
/// </remarks>
public class TempPermissionHandler : AuthorizationHandler<TempPermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TempPermissionRequirement requirement)
    {
        var hasPermission = context.User.FindAll(TempClaimTypes.Permission)
            .Any(c => c.Value.Equals(requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (!hasPermission)
        {
            return Task.CompletedTask; // 没有该临时权限 → 不 Succeed → 403
        }

        var now = DateTimeOffset.UtcNow;
        var prefix = requirement.Permission + "|";

        foreach (var untilClaim in context.User.FindAll(TempClaimTypes.ValidUntil))
        {
            // 只认「本权限编码|时效」这种成对格式
            if (!untilClaim.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var untilRaw = untilClaim.Value[(requirement.Permission.Length + 1)..];
            if (!DateTimeOffset.TryParse(
                    untilRaw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var until))
            {
                continue;
            }

            if (until > now)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        // 有权限声明但对应时效已过 → 不 Succeed（自动失效）
        return Task.CompletedTask;
    }
}
