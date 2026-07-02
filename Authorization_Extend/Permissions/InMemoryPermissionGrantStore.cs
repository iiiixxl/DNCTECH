namespace Authorization_Extend.Permissions;

/// <summary>
/// 模拟 AbpPermissionGrants 表：角色 → 权限名 的授予关系。
/// </summary>
public class InMemoryPermissionGrantStore : IPermissionGrantStore
{
    private readonly HashSet<(string Role, string Permission)> _grants = new();

    public InMemoryPermissionGrantStore()
    {
        Seed();
    }

    public Task<IReadOnlyList<string>> GetGrantedPermissionsAsync(string roleName)
    {
        var permissions = _grants
            .Where(x => x.Role.Equals(roleName, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Permission)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(permissions);
    }

    public Task GrantAsync(string roleName, string permissionName)
    {
        _grants.Add((roleName, permissionName));
        return Task.CompletedTask;
    }

    public Task RevokeAsync(string roleName, string permissionName)
    {
        _grants.Remove((roleName, permissionName));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetRolesAsync()
    {
        IReadOnlyList<string> roles = _grants.Select(x => x.Role).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return Task.FromResult(roles);
    }

    private void Seed()
    {
        // Admin：静态 + 预置报表权限全开
        foreach (var permission in new[]
                 {
                     PermissionNames.Books.Default,
                     PermissionNames.Books.Create,
                     PermissionNames.Books.Update,
                     PermissionNames.Books.Delete,
                     PermissionNames.Reports.Create,
                     PermissionNames.Reports.Delete,
                     PermissionNames.Reports.GetViewPermission("SALES_DAILY"),
                     PermissionNames.Reports.GetViewPermission("STOCK_MONTHLY")
                 })
        {
            _grants.Add(("Admin", permission));
        }

        // User：只能看图书 + 销售日报
        _grants.Add(("User", PermissionNames.Books.Default));
        _grants.Add(("User", PermissionNames.Reports.GetViewPermission("SALES_DAILY")));
    }
}
