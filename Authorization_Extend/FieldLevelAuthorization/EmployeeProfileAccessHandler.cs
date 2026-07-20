using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>
/// 员工档案行级授权：员工只能查看自己的档案，Admin（演示中的 HR）可查看任意员工档案。
/// 字段可见性仍由 FieldAccessHandler 另行决定。
/// </summary>
public sealed class EmployeeProfileAccessHandler
    : AuthorizationHandler<EmployeeProfileAccessRequirement, Employee>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EmployeeProfileAccessRequirement requirement,
        Employee resource)
    {
        var currentUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isOwner = string.Equals(currentUserId, resource.UserId, StringComparison.OrdinalIgnoreCase);
        var isHr = context.User.IsInRole("Admin");

        if (isOwner || isHr)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
