using Microsoft.Extensions.Options;

namespace DynamicOption_Extend.DynamicOptions;

/// <summary>
/// 仿 ABP <c>OptionsAbpDynamicOptionsManagerExtensions</c>：对注入的 <see cref="IOptions{T}"/>
/// 调用 <see cref="DynamicOptionsManager{T}.SetAsync"/> 触发一次动态覆盖。
/// </summary>
public static class OptionsDynamicOptionsExtensions
{
    public static Task SetAsync<T>(this IOptions<T> options)
        where T : class
    {
        return options.ToDynamicOptions().SetAsync();
    }

    public static Task SetAsync<T>(this IOptions<T> options, string name)
        where T : class
    {
        return options.ToDynamicOptions().SetAsync(name);
    }

    private static DynamicOptionsManager<T> ToDynamicOptions<T>(this IOptions<T> options)
        where T : class
    {
        if (options is DynamicOptionsManager<T> manager)
        {
            return manager;
        }

        throw new DynamicOptionsException(
            $"IOptions<{typeof(T).Name}> 必须由 {typeof(DynamicOptionsManager<>).FullName} 实现。" +
            "请调用 services.AddDynamicOptions<TOptions, TManager>() 注册。");
    }
}
