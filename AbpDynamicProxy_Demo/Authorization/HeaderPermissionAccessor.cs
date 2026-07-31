namespace AbpDynamicProxy_Demo.Authorization;

/// <summary>
/// 当前请求持有的权限（演示用：从 Header X-Permissions 读取）。
/// </summary>
public interface ICurrentPermissionAccessor
{
    IReadOnlyCollection<string> GetGrantedPermissions();
}

public class HeaderPermissionAccessor : ICurrentPermissionAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeaderPermissionAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IReadOnlyCollection<string> GetGrantedPermissions()
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers["X-Permissions"].ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return Array.Empty<string>();
        }

        return header
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
