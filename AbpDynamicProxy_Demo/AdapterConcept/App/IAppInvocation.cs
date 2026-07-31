namespace AbpDynamicProxy_Demo.AdapterConcept.App;

/// <summary>
/// 我们自己定义的「一次调用」抽象（类似 ABP 的 IAbpMethodInvocation）。
/// 业务横切只依赖这一套，不依赖 Foreign*。
/// </summary>
public interface IAppInvocation
{
    string MethodName { get; }

    object[] Args { get; }

    object? ReturnValue { get; set; }

    /// <summary>
    /// 我们喜欢 async；内部再去调第三方的 Continue。
    /// </summary>
    Task ProceedAsync();
}
