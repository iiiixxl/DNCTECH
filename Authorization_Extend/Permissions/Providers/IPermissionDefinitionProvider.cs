using Authorization_Extend.Permissions.Definitions;

namespace Authorization_Extend.Permissions.Providers;

public interface IPermissionDefinitionProvider
{
    void Define(PermissionDefinitionContext context);
}
