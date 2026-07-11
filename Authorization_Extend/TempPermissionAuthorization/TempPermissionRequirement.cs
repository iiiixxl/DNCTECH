using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.TempPermissionAuthorization;

/// <summary>
/// 临时权限要求：携带要校验的临时权限编码（如 expense.approve）。
/// 本身不干活，只当「诉求单」；真正判断交给 TempPermissionHandler。
/// </summary>
public class TempPermissionRequirement : IAuthorizationRequirement
{
    public TempPermissionRequirement(string permission)
    {
        Permission = permission;
    }

    /// <summary>要求持有的临时权限编码。</summary>
    public string Permission { get; }
}
