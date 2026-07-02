using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.Permissions.Authorization;

/// <summary>
/// 声明式标记权限，Policy 名 = 权限名，底层走统一的 PermissionAuthorizationHandler。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionName)
    {
        Policy = permissionName;
    }
}
