using Authorization_Extend.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.Permissions.Authorization;

/// <summary>
/// 唯一 Handler：所有动态权限校验都走这里，不需要为每个权限注册 Policy。
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    private readonly IPermissionChecker _permissionChecker;

    public PermissionAuthorizationHandler(IPermissionChecker permissionChecker)
    {
        _permissionChecker = permissionChecker;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement)
    {
        if (await _permissionChecker.IsGrantedAsync(context.User, requirement.PermissionName))
        {
            context.Succeed(requirement);
        }
    }
}
