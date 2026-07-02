using DynamicOption_Extend.DynamicOptions;
using DynamicOption_Extend.Models;
using DynamicOption_Extend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DynamicOption_Extend;

internal static class Program
{
    private static async Task Main()
    {
        Console.WriteLine("=== DynamicOptions Demo（仿 ABP AbpDynamicOptionsManager 原理）===\n");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();

        // 1. 静态基线：来自 appsettings.json（等价于启动时 Configure）
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection("Email"));

        // 2. 动态设置存储（模拟 DB / 设置中心）
        services.AddSingleton<InMemorySettingStore>();

        // 3. 用自定义 Manager 替换 IOptions / IOptionsSnapshot（核心一步）
        services.AddDynamicOptions<EmailOptions, EmailOptionsManager>();

        services.AddScoped<EmailService>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var emailService = scopedProvider.GetRequiredService<EmailService>();
        var options = scopedProvider.GetRequiredService<IOptions<EmailOptions>>();
        var settingStore = scopedProvider.GetRequiredService<InMemorySettingStore>();

        // 未调用 SetAsync 前：仍是 appsettings 的静态值
        emailService.PrintCurrentConfig("步骤1 - 仅 appsettings，未同步动态设置");

        // 4. 集中调用一次 SetAsync，把设置存储覆盖到当前 Scope 的 Options 实例
        await options.SetAsync();
        emailService.PrintCurrentConfig("步骤2 - 调用 SetAsync() 后，动态设置已覆盖");

        // 5. 模拟管理员在 UI 修改了设置
        Console.WriteLine(">>> 模拟：管理员在后台把 SmtpHost 改为 smtp.updated.local\n");
        settingStore.Set("Email.SmtpHost", "smtp.updated.local");

        // 6. 再次集中同步，所有注入 IOptions<EmailOptions> 的服务立即读到新值
        await options.SetAsync();
        emailService.PrintCurrentConfig("步骤3 - 设置变更后再次 SetAsync()，无需重启");

        // 7. 同一 Scope 内多个消费者共享同一份已更新的 Options 实例
        var anotherEmailService = scopedProvider.GetRequiredService<EmailService>();
        anotherEmailService.PrintCurrentConfig("步骤4 - 同 Scope 另一服务，值已一致");

        Console.WriteLine("Demo 完成。原理：DynamicOptionsManager 继承 OptionsManager，");
        Console.WriteLine("SetAsync → OverrideOptionsAsync 原地修改实例；");
        Console.WriteLine("AddDynamicOptions 将 IOptions/IOptionsSnapshot 替换为该 Manager。");
    }
}
