using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.Permissions.Authorization;

public class PermissionAuthorizationRequirement : IAuthorizationRequirement
{
    public PermissionAuthorizationRequirement(string permissionName)
    {
        PermissionName = permissionName;
    }

    public string PermissionName { get; }
}
