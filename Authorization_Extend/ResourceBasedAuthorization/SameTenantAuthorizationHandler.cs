using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 场景一「多租户隔离」的裁判。
/// 注意泛型第二个参数是 <see cref="ITenantScoped"/> 接口而非 Order：
/// 任何实现了该接口的实体（订单、合同…）都会路由到这里，一个 Handler 覆盖全业务。
/// </summary>
public class SameTenantAuthorizationHandler : AuthorizationHandler<SameTenantRequirement, ITenantScoped>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameTenantRequirement requirement,
        ITenantScoped resource)
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
