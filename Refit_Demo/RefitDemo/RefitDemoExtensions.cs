using Refit;
using Refit_Demo.RefitDemo.Contracts;
using Refit_Demo.RefitDemo.FakeRemote;
using Refit_Demo.RefitDemo.Handlers;
using Refit_Demo.RefitDemo.Options;

namespace Refit_Demo.RefitDemo;

public static class RefitDemoExtensions
{
    public static IServiceCollection AddRefitDemo(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RefitDemoOptions>(configuration.GetSection(RefitDemoOptions.SectionName));
        services.AddSingleton<InMemoryTodoStore>();
        services.AddHttpContextAccessor();

        services.AddTransient<DemoAuthHeaderHandler>();
        services.AddTransient<DemoTelemetryHandler>();

        var options = configuration.GetSection(RefitDemoOptions.SectionName).Get<RefitDemoOptions>()
                      ?? new RefitDemoOptions();

        // Handler 注册顺序：后注册的更靠近调用方 → 请求先经 Telemetry，再 Auth，再出网
        services.AddRefitClient<IDemoTodoApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(options.BaseAddress);
                c.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DemoAuthHeaderHandler>()
            .AddHttpMessageHandler<DemoTelemetryHandler>();

        return services;
    }
}
