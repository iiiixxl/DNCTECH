namespace Authorization_Extend.Permissions;

/// <summary>
/// 角色 → 权限名 的授予关系存储（模拟 AbpPermissionGrants 表）。
/// </summary>
public interface IPermissionGrantStore
{
    Task<IReadOnlyList<string>> GetGrantedPermissionsAsync(string roleName);

    Task GrantAsync(string roleName, string permissionName);

    Task RevokeAsync(string roleName, string permissionName);

    Task<IReadOnlyList<string>> GetRolesAsync();
}
