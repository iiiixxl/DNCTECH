namespace DynamicOption_Extend.Services;

/// <summary>
/// 模拟数据库 / 设置中心中的动态配置（类似 ABP 的 ISettingManager 存储）。
/// </summary>
public class InMemorySettingStore
{
    private readonly Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Email.SmtpHost"] = "smtp.dynamic.local",
        ["Email.SmtpPort"] = "587",
        ["Email.SenderAddress"] = "admin@dynamic.local",
        ["Email.EnableSsl"] = "true"
    };

    public string? GetOrNull(string key) =>
        _settings.TryGetValue(key, out var value) ? value : null;

    /// <summary>模拟管理员在后台修改设置。</summary>
    public void Set(string key, string value) => _settings[key] = value;
}
