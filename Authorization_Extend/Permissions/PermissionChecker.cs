using System.Security.Claims;

namespace Authorization_Extend.Permissions;

/// <summary>
/// 仿 ABP IPermissionChecker：用户 Claims 中的 Role → 查 GrantStore → 是否拥有权限。
/// </summary>
public class PermissionChecker : IPermissionChecker
{
    private readonly IPermissionGrantStore _grantStore;

    public PermissionChecker(IPermissionGrantStore grantStore)
    {
        _grantStore = grantStore;
    }

    public async Task<bool> IsGrantedAsync(ClaimsPrincipal user, string permissionName)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            var granted = await _grantStore.GetGrantedPermissionsAsync(role);
            if (granted.Any(p => p.Equals(permissionName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
