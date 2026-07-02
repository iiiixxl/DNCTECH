using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.PolicyCodeAuthorization;

/// <summary>
/// 声明式标记权限编码，等价于 [Authorize(Policy = "User.Delete")]。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionCodeAttribute : AuthorizeAttribute
{
    public RequirePermissionCodeAttribute(string permissionCode)
    {
        Policy = permissionCode;
    }
}
