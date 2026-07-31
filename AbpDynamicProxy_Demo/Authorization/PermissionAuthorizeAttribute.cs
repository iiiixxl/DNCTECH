namespace AbpDynamicProxy_Demo.Authorization;

/// <summary>
/// 简化版「权限 Authorize」。对应 ASP.NET [Authorize(Policy = "...")] / ABP 权限名。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class PermissionAuthorizeAttribute : Attribute
{
    public string PermissionName { get; }

    public PermissionAuthorizeAttribute(string permissionName)
    {
        PermissionName = permissionName;
    }
}
