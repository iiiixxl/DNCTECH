using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 场景二「数据所有权」的裁判。
/// 泛型 &lt;OrderOwnerRequirement, Order&gt; 同样声明了「我要拿到具体的 Order 资源」。
/// 拿订单的 OwnerUserId 和当前用户 Id 比对，避免「有 orders.refund 功能权限，却去退别人订单」的越权。
/// </summary>
public class OrderOwnerAuthorizationHandler : AuthorizationHandler<OrderOwnerRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderOwnerRequirement requirement,
        Order resource)
    {
        // 认证阶段写入的当前用户 Id
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // 订单创建者 == 当前用户，才放行；否则就是操作他人数据的越权
        if (!string.IsNullOrEmpty(userId) &&
            string.Equals(userId, resource.OwnerUserId, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
