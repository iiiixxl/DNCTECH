using Authorization_Extend.Permissions.Definitions;

namespace Authorization_Extend.Permissions.Providers;

public abstract class PermissionDefinitionProvider : IPermissionDefinitionProvider
{
    public abstract void Define(PermissionDefinitionContext context);
}
