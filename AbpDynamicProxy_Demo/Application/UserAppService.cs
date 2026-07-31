using AbpDynamicProxy_Demo.Authorization;

namespace AbpDynamicProxy_Demo.Application;

/// <summary>
/// 对应 ABP IdentityUserAppService：
/// 类上 Default + 方法上 Create/Delete，鉴权由 AuthorizationInterceptor 完成。
/// </summary>
[PermissionAuthorize(UserPermissions.Default)]
public class UserAppService : IUserAppService
{
    private static readonly List<string> Users = new() { "admin", "alice" };

    public virtual Task<IReadOnlyList<string>> GetListAsync()
    {
        return Task.FromResult<IReadOnlyList<string>>(Users.ToList());
    }

    [PermissionAuthorize(UserPermissions.Create)]
    public virtual Task<string> CreateAsync(string userName)
    {
        if (!Users.Contains(userName, StringComparer.OrdinalIgnoreCase))
        {
            Users.Add(userName);
        }

        return Task.FromResult(userName);
    }

    [PermissionAuthorize(UserPermissions.Delete)]
    public virtual Task DeleteAsync(string userName)
    {
        Users.RemoveAll(x => string.Equals(x, userName, StringComparison.OrdinalIgnoreCase));
        return Task.CompletedTask;
    }
}
