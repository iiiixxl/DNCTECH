namespace Authorization_Extend.TempPermissionAuthorization;

/// <summary>
/// 内存模拟 temp_permission_grants 表：key = userId，value = 该用户的授权列表。
/// </summary>
public class InMemoryTempPermissionStore : ITempPermissionStore
{
    private readonly object _lock = new();
    private readonly Dictionary<string, List<TempPermissionGrant>> _grants = new(StringComparer.OrdinalIgnoreCase);

    public Task GrantAsync(TempPermissionGrant grant)
    {
        lock (_lock)
        {
            if (!_grants.TryGetValue(grant.GranteeUserId, out var list))
            {
                list = [];
                _grants[grant.GranteeUserId] = list;
            }

            // 同权限覆盖：主管重新授权时刷新时效
            list.RemoveAll(g =>
                g.Permission.Equals(grant.Permission, StringComparison.OrdinalIgnoreCase));
            list.Add(grant);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TempPermissionGrant>> GetActiveGrantsAsync(string userId)
    {
        lock (_lock)
        {
            if (!_grants.TryGetValue(userId, out var list))
            {
                return Task.FromResult<IReadOnlyList<TempPermissionGrant>>([]);
            }

            var active = list.Where(g => g.IsActive).ToList();
            return Task.FromResult<IReadOnlyList<TempPermissionGrant>>(active);
        }
    }

    public Task<IReadOnlyList<TempPermissionGrant>> GetAllGrantsAsync(string userId)
    {
        lock (_lock)
        {
            if (!_grants.TryGetValue(userId, out var list))
            {
                return Task.FromResult<IReadOnlyList<TempPermissionGrant>>([]);
            }

            return Task.FromResult<IReadOnlyList<TempPermissionGrant>>(list.ToList());
        }
    }

    public Task RevokeAsync(string userId, string permission)
    {
        lock (_lock)
        {
            if (_grants.TryGetValue(userId, out var list))
            {
                list.RemoveAll(g =>
                    g.Permission.Equals(permission, StringComparison.OrdinalIgnoreCase));
            }
        }

        return Task.CompletedTask;
    }
}
