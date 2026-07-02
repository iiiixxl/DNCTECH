namespace Authorization_Extend.PolicyCodeAuthorization;

/// <summary>
/// 内存模拟 user_permissions 表：userId → permissionCode。
/// </summary>
public class InMemoryUserPermissionService : IUserPermissionService
{
    private readonly Dictionary<string, HashSet<string>> _userPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["user-admin"] =
        [
            PolicyCodePermissionNames.UserView,
            PolicyCodePermissionNames.UserDelete,
            PolicyCodePermissionNames.OrderCreate
        ],
        ["user-normal"] =
        [
            PolicyCodePermissionNames.UserView
        ]
    };

    public Task<bool> UserHasPermissionAsync(string userId, string permissionCode)
    {
        if (!_userPermissions.TryGetValue(userId, out var permissions))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(permissions.Contains(permissionCode));
    }

    public Task<IReadOnlyList<string>> GetUserPermissionsAsync(string userId)
    {
        if (!_userPermissions.TryGetValue(userId, out var permissions))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        return Task.FromResult<IReadOnlyList<string>>(permissions.ToList());
    }
}
