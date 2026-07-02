namespace Authorization_Extend.Permissions.Definitions;

/// <summary>
/// 单个权限定义项（静态或动态）。
/// </summary>
public class PermissionDefinition
{
    public PermissionDefinition(string name, string displayName, string groupName)
    {
        Name = name;
        DisplayName = displayName;
        GroupName = groupName;
    }

    public string Name { get; }

    public string DisplayName { get; }

    public string GroupName { get; }

    /// <summary>是否为运行时动态注册的权限（如新增报表后自动生成的 View 权限）。</summary>
    public bool IsDynamic { get; init; }
}
