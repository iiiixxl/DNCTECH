using Microsoft.Extensions.Options;

namespace DynamicOption_Extend.DynamicOptions;

/// <summary>
/// 仿 ABP <c>AbpDynamicOptionsManager&lt;T&gt;</c>：在标准 Options 管道之上，
/// 通过 <see cref="OverrideOptionsAsync"/> 在运行时覆盖已构建的 Options 实例。
/// </summary>
/// <remarks>
/// 工作流程：
/// 1. 框架先通过 IOptionsFactory 从 appsettings 等静态源构建 Options
/// 2. 调用 <see cref="SetAsync"/> 触发 <see cref="OverrideOptionsAsync"/>，原地修改实例
/// 3. 同一 Scope 内所有注入 IOptions&lt;T&gt; 的服务共享这份已更新的实例
/// </remarks>
public abstract class DynamicOptionsManager<T> : OptionsManager<T>
    where T : class
{
    protected DynamicOptionsManager(IOptionsFactory<T> factory)
        : base(factory)
    {
    }

    /// <summary>对默认命名的 Options 执行一次动态覆盖。</summary>
    public Task SetAsync() => SetAsync(Options.DefaultName);

    /// <summary>对指定命名的 Options 执行一次动态覆盖。</summary>
    public virtual Task SetAsync(string name)
    {
        return OverrideOptionsAsync(name, Get(name));
    }

    /// <summary>
    /// 子类实现：从数据库 / 设置中心等动态来源读取值，覆盖到 options 实例上。
    /// </summary>
    protected abstract Task OverrideOptionsAsync(string name, T options);
}
