using System.Collections.Concurrent;

namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>活跃会话。对应 ABP <c>IdentitySession</c>。</summary>
public class UserSession
{
    public required string SessionId { get; init; }
    public required string Username { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastAccessed { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// JWT session 白名单。对应 ABP <c>IdentitySession</c> / <c>IdentitySessionManager</c>。
/// </summary>
public class UserSessionStore
{
    private readonly ConcurrentDictionary<string, UserSession> _sessions = new(StringComparer.Ordinal);

    public string Create(string username)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        Save(sessionId, username);
        return sessionId;
    }

    /// <summary>
    /// 对应 ABP 登录成功后把已有 session_id 写入 IdentitySession（OnSignedIn）。
    /// </summary>
    public void Save(string sessionId, string username) =>
        _sessions[sessionId] = new UserSession { SessionId = sessionId, Username = username };

    public bool IsValid(string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) || !_sessions.TryGetValue(sessionId, out var session))
        {
            return false;
        }

        session.LastAccessed = DateTimeOffset.UtcNow;
        return true;
    }

    public void Revoke(string sessionId) => _sessions.TryRemove(sessionId, out _);

    public int RevokeAll(string username)
    {
        var keys = _sessions
            .Where(x => x.Value.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Key)
            .ToList();

        foreach (var key in keys)
        {
            _sessions.TryRemove(key, out _);
        }

        return keys.Count;
    }

    public IReadOnlyList<UserSession> GetByUser(string username) =>
        _sessions.Values
            .Where(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.LastAccessed)
            .ToList();
}
