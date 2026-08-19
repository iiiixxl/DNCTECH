using System.Collections.Concurrent;

namespace SessionExpiredRace_Demo;

public sealed record SessionRow(Guid SessionId, DateTime CreatedAtUtc);

/// <summary>
/// 伪“AbpSessions 表”：只暴露 committed 的结果（模拟另一个请求只能读到已提交行）。
/// </summary>
public static class FakeSessionStore
{
    private static readonly ConcurrentDictionary<Guid, SessionRow> _committed = new();

    public static void Clear() => _committed.Clear();

    public static bool TryGet(Guid sessionId, out SessionRow row) =>
        _committed.TryGetValue(sessionId, out row!);

    public static void Commit(Guid sessionId)
    {
        _committed.TryAdd(sessionId, new SessionRow(sessionId, DateTime.UtcNow));
    }

    public static IReadOnlyCollection<SessionRow> GetAll() => _committed.Values.ToArray();
}

