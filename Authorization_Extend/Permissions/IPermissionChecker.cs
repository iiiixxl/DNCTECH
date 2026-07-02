using System.Security.Claims;

namespace Authorization_Extend.Permissions;

public interface IPermissionChecker
{
    Task<bool> IsGrantedAsync(ClaimsPrincipal user, string permissionName);
}
