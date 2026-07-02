namespace Authorization_Extend.Permissions.Definitions;

/// <summary>
/// Provider 声明权限时的上下文，用于收集权限分组与权限项。
/// </summary>
public class PermissionDefinitionContext
{
    private readonly Dictionary<string, PermissionGroupDefinition> _groups = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<PermissionGroupDefinition> Groups => _groups.Values;

    public PermissionGroupDefinition AddGroup(string name, string displayName)
    {
        if (_groups.TryGetValue(name, out var existing))
        {
            return existing;
        }

        var group = new PermissionGroupDefinition(name, displayName);
        _groups[name] = group;
        return group;
    }

    public IEnumerable<PermissionDefinition> GetAllPermissions()
    {
        return _groups.Values.SelectMany(g => g.Permissions);
    }
}
