using System.Reflection;

namespace AbpDynamicProxy_Demo.Authorization;

/// <summary>
/// 对应 ABP: MethodInvocationAuthorizationService
/// 根据当前 MethodInfo 上的 + 声明类型上的 Authorize 特性做校验。
/// </summary>
public interface IMethodInvocationAuthorizationService
{
    Task CheckAsync(MethodInfo method);
}

public class MethodInvocationAuthorizationService : IMethodInvocationAuthorizationService
{
    private readonly ICurrentPermissionAccessor _permissionAccessor;

    public MethodInvocationAuthorizationService(ICurrentPermissionAccessor permissionAccessor)
    {
        _permissionAccessor = permissionAccessor;
    }

    public Task CheckAsync(MethodInfo method)
    {
        var requiredPermissions = GetRequiredPermissions(method);
        if (requiredPermissions.Count == 0)
        {
            return Task.CompletedTask;
        }

        var granted = _permissionAccessor.GetGrantedPermissions();
        foreach (var permission in requiredPermissions)
        {
            if (!granted.Contains(permission))
            {
                throw new AuthorizationException(permission);
            }
        }

        return Task.CompletedTask;
    }

    private static List<string> GetRequiredPermissions(MethodInfo methodInfo)
    {
        var attributes = methodInfo
            .GetCustomAttributes(true)
            .OfType<PermissionAuthorizeAttribute>()
            .ToList();

        if (methodInfo.IsPublic && methodInfo.DeclaringType != null)
        {
            var typeAttributes = methodInfo.DeclaringType
                .GetCustomAttributes(true)
                .OfType<PermissionAuthorizeAttribute>();

            attributes.AddRange(typeAttributes);
        }

        return attributes
            .Select(x => x.PermissionName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
