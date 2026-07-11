using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 场景一「多租户隔离」的裁判。
/// 注意泛型第二个参数 &lt;SameTenantRequirement, Order&gt;：这正是「基于资源」的关键——
/// 框架会把 AuthorizeAsync 时传进来的那个 Order 实例直接送到这里，
/// 我们拿订单的 TenantId 和用户 Claim 里的 tenant_id 做比对。
/// </summary>
public class SameTenantAuthorizationHandler : AuthorizationHandler<SameTenantRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameTenantRequirement requirement,
        Order resource)
    {
        // 从认证阶段写入的 Claim 取当前用户所属租户
        var tenantId = context.User.FindFirst(ResourceClaimTypes.TenantId)?.Value;

        // 订单的租户 == 用户的租户，才放行；否则就是跨租户越权
        if (!string.IsNullOrEmpty(tenantId) &&
            string.Equals(tenantId, resource.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
