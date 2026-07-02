using Authorization_Extend.Permissions.Definitions;

namespace Authorization_Extend.Permissions;

public interface IPermissionDefinitionManager
{
    IReadOnlyList<PermissionDefinition> GetAll();

    bool Exists(string permissionName);

    void AddDynamic(PermissionDefinition permission);
}
