using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.PolicyCodeAuthorization;

/// <summary>
/// 权限要求：携带权限编码（如 User.Delete），由 Handler 查库验证用户是否拥有。
/// </summary>
public class PermissionCodeRequirement : IAuthorizationRequirement
{
    public PermissionCodeRequirement(string permissionCode)
    {
        PermissionCode = permissionCode;
    }

    public string PermissionCode { get; }
}
