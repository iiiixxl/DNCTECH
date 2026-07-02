using Authorization_Extend.Permissions.Definitions;
using Authorization_Extend.Permissions.Providers;

namespace Authorization_Extend.Permissions;

/// <summary>
/// 启动时收集所有 Provider 声明的权限；运行时可追加动态权限定义。
/// </summary>
public class PermissionDefinitionManager : IPermissionDefinitionManager
{
    private readonly Dictionary<string, PermissionDefinition> _permissions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public PermissionDefinitionManager(IEnumerable<IPermissionDefinitionProvider> providers)
    {
        var context = new PermissionDefinitionContext();
        foreach (var provider in providers)
        {
            provider.Define(context);
        }

        foreach (var permission in context.GetAllPermissions())
        {
            _permissions[permission.Name] = permission;
        }
    }

    public IReadOnlyList<PermissionDefinition> GetAll()
    {
        lock (_lock)
        {
            return _permissions.Values.ToList();
        }
    }

    public bool Exists(string permissionName)
    {
        lock (_lock)
        {
            return _permissions.ContainsKey(permissionName);
        }
    }

    public void AddDynamic(PermissionDefinition permission)
    {
        lock (_lock)
        {
            var dynamicPermission = new PermissionDefinition(
                permission.Name,
                permission.DisplayName,
                permission.GroupName)
            {
                IsDynamic = true
            };
            _permissions[dynamicPermission.Name] = dynamicPermission;
        }
    }
}
