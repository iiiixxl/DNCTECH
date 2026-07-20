using System.Collections.Concurrent;

namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>
/// 用户角色权威源（内存版）。对应 ABP Identity 里用户/角色表 + 动态声明缓存。
/// Token 里的 Role 只是签发时快照；每次请求由 Contributor 用这里的最新值覆盖。
/// </summary>
public class DemoUserClaimStore
{
    private readonly ConcurrentDictionary<string, string[]> _roles = new(StringComparer.OrdinalIgnoreCase);

    public DemoUserClaimStore()
    {
        _roles["admin"] = ["Admin", "User"];
    }

    public string[] GetRoles(string username) =>
        _roles.TryGetValue(username, out var roles) ? roles : [];

    public void SetRoles(string username, string[] roles) =>
        _roles[username] = roles;
}
