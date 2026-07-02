using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.PolicyCodeAuthorization;

/// <summary>
/// 极简权限 Handler：从 Claim 取 userId → 查 IUserPermissionService 是否拥有权限编码。
/// </summary>
/// <remarks>
/// 与 ABP 风格对比：
/// - ABP：Role → GrantStore → 权限名
/// - 本方案：UserId → 数据库 → 权限编码（少一层角色映射，代码更少）
/// </remarks>
public class PermissionCodeHandler : AuthorizationHandler<PermissionCodeRequirement>
{
    private readonly IUserPermissionService _permissionService;

    public PermissionCodeHandler(IUserPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionCodeRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        if (await _permissionService.UserHasPermissionAsync(userId, requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }
    }
}
