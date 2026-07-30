using System.Collections.Concurrent;

namespace RulesEngine_Demo.Approaches.ConfigStore;

/// <summary>
/// 模拟「数据库 / 配置中心」：按 Workflow 名称保存版本化 JSON 字符串。
/// </summary>
public sealed class InMemoryRuleStore
{
    private readonly ConcurrentDictionary<string, (int Version, string Json)> _items = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> GetAllJson() =>
        _items.Values.Select(v => v.Json).ToList();

    public void Upsert(string workflowName, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        _items.AddOrUpdate(
            workflowName,
            _ => (1, json),
            (_, existing) => (existing.Version + 1, json));
    }

    public int GetVersion(string workflowName) =>
        _items.TryGetValue(workflowName, out var item) ? item.Version : 0;

    public bool TryGetJson(string workflowName, out string json)
    {
        if (_items.TryGetValue(workflowName, out var item))
        {
            json = item.Json;
            return true;
        }

        json = "";
        return false;
    }

    public IReadOnlyDictionary<string, int> SnapshotVersions() =>
        _items.ToDictionary(kv => kv.Key, kv => kv.Value.Version, StringComparer.OrdinalIgnoreCase);
}
