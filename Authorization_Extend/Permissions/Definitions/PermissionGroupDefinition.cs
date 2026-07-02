namespace Authorization_Extend.Permissions.Definitions;

/// <summary>
/// 权限分组，用于 UI 权限树展示。
/// </summary>
public class PermissionGroupDefinition
{
    public PermissionGroupDefinition(string name, string displayName)
    {
        Name = name;
        DisplayName = displayName;
    }

    public string Name { get; }

    public string DisplayName { get; }

    public List<PermissionDefinition> Permissions { get; } = [];

    public PermissionDefinition AddPermission(string name, string displayName)
    {
        var permission = new PermissionDefinition(name, displayName, Name);
        Permissions.Add(permission);
        return permission;
    }
}
