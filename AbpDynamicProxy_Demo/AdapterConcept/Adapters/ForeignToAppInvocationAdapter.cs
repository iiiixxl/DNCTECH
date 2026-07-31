using AbpDynamicProxy_Demo.AdapterConcept.App;
using AbpDynamicProxy_Demo.AdapterConcept.Foreign;

namespace AbpDynamicProxy_Demo.AdapterConcept.Adapters;

/// <summary>
/// ★ 适配器②：调用上下文适配
/// ForeignInvocation（第三方） → IAppInvocation（我们）
/// 对应 ABP: CastleAbpMethodInvocationAdapter
/// </summary>
public class ForeignToAppInvocationAdapter : IAppInvocation
{
    private readonly ForeignInvocation _foreign;

    public ForeignToAppInvocationAdapter(ForeignInvocation foreign)
    {
        _foreign = foreign;
    }

    public string MethodName => _foreign.MethodName;

    public object[] Args => _foreign.Args;

    public object? ReturnValue
    {
        get => _foreign.Result;
        set => _foreign.Result = value;
    }

    public Task ProceedAsync()
    {
        // 把「我们的 ProceedAsync」翻译成「第三方的 Continue」
        _foreign.Continue();
        return Task.CompletedTask;
    }
}
