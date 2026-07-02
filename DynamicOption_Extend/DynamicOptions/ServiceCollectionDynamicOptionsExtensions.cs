using DynamicOption_Extend.DynamicOptions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 仿 ABP <c>AddAbpDynamicOptions</c>：用自定义 <see cref="DynamicOptionsManager{T}"/> 替换
/// <see cref="IOptions{TOptions}"/> 与 <see cref="IOptionsSnapshot{TOptions}"/> 的默认实现。
/// </summary>
/// <remarks>
/// 放在 Microsoft.Extensions.DependencyInjection 命名空间下，
/// 使 services.AddDynamicOptions(...) 与 AddOptions 等扩展方法风格一致。
/// </remarks>
public static class ServiceCollectionDynamicOptionsExtensions
{
    public static IServiceCollection AddDynamicOptions<TOptions, TManager>(this IServiceCollection services)
        where TOptions : class
        where TManager : DynamicOptionsManager<TOptions>
    {
        services.Replace(ServiceDescriptor.Scoped(typeof(IOptions<TOptions>), typeof(TManager)));
        services.Replace(ServiceDescriptor.Scoped(typeof(IOptionsSnapshot<TOptions>), typeof(TManager)));

        return services;
    }
}
