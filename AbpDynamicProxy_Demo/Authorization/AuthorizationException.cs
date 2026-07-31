namespace AbpDynamicProxy_Demo.Authorization;

public class AuthorizationException : Exception
{
    public string PermissionName { get; }

    public AuthorizationException(string permissionName)
        : base($"Authorization failed. Required permission: {permissionName}")
    {
        PermissionName = permissionName;
    }
}
