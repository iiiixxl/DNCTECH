namespace SessionExpiredRace_Demo;

public sealed class FakeTransaction
{
    private readonly HashSet<Guid> _stagedSessionIds = new();

    public void StageInsert(Guid sessionId)
    {
        _stagedSessionIds.Add(sessionId);
    }

    public bool HasStaged => _stagedSessionIds.Count > 0;

    public void Commit()
    {
        foreach (var id in _stagedSessionIds)
        {
            FakeSessionStore.Commit(id);
        }

        _stagedSessionIds.Clear();
    }

    public void Rollback()
    {
        _stagedSessionIds.Clear();
    }
}

