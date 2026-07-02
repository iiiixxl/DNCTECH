using DynamicOption_Extend.DynamicOptions;
using DynamicOption_Extend.Models;
using Microsoft.Extensions.Options;

namespace DynamicOption_Extend.Services;

/// <summary>
/// 仿 ABP Identity 模块的 OptionsManager：静态配置来自 appsettings，
/// 运行时由 <see cref="InMemorySettingStore"/> 覆盖。
/// </summary>
public class EmailOptionsManager : DynamicOptionsManager<EmailOptions>
{
    private readonly InMemorySettingStore _settingStore;

    public EmailOptionsManager(
        IOptionsFactory<EmailOptions> factory,
        InMemorySettingStore settingStore)
        : base(factory)
    {
        _settingStore = settingStore;
    }

    protected override Task OverrideOptionsAsync(string name, EmailOptions options)
    {
        if (TryGetString("Email.SmtpHost", out var smtpHost))
        {
            options.SmtpHost = smtpHost;
        }

        if (TryGetInt("Email.SmtpPort", out var smtpPort))
        {
            options.SmtpPort = smtpPort;
        }

        if (TryGetString("Email.SenderAddress", out var senderAddress))
        {
            options.SenderAddress = senderAddress;
        }

        if (TryGetBool("Email.EnableSsl", out var enableSsl))
        {
            options.EnableSsl = enableSsl;
        }

        return Task.CompletedTask;
    }

    private bool TryGetString(string key, out string value)
    {
        var stored = _settingStore.GetOrNull(key);
        if (stored is null)
        {
            value = string.Empty;
            return false;
        }

        value = stored;
        return true;
    }

    private bool TryGetInt(string key, out int value)
    {
        var stored = _settingStore.GetOrNull(key);
        if (stored is null || !int.TryParse(stored, out value))
        {
            value = 0;
            return false;
        }

        return true;
    }

    private bool TryGetBool(string key, out bool value)
    {
        var stored = _settingStore.GetOrNull(key);
        if (stored is null || !bool.TryParse(stored, out value))
        {
            value = false;
            return false;
        }

        return true;
    }
}
