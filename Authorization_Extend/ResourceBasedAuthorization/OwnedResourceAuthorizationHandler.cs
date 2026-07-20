using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 通用「数据所有权」裁判：资源归属人 ≠ 当前用户 → 不 Succeed → 403。
/// 泛型第二参数是 <see cref="IOwnedResource"/> 接口，因此 Order、Contract、工单等
/// 只要实现该接口并传给 AuthorizeAsync，都会路由到这里，不用每加一个业务表就写 Handler。
/// </summary>
public class OwnedResourceAuthorizationHandler : AuthorizationHandler<OwnedResourceRequirement, IOwnedResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnedResourceRequirement requirement,
        IOwnedResource resource)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId) &&
            string.Equals(userId, resource.OwnerUserId, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
