using System.Reflection;
using Castle.DynamicProxy;

namespace AbpDynamicProxy_Demo.DynamicProxy;

/// <summary>
/// 对应 ABP: CastleAbpMethodInvocationAdapter
/// 把 Castle 的 IInvocation 适配成 ABP 的 IAbpMethodInvocation。
/// </summary>
public class CastleAbpMethodInvocationAdapter : IAbpMethodInvocation
{
    private readonly IInvocation _invocation;
    private readonly IInvocationProceedInfo _proceedInfo;
    private readonly Func<IInvocation, IInvocationProceedInfo, Task> _proceed;

    public CastleAbpMethodInvocationAdapter(
        IInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task> proceed)
    {
        _invocation = invocation;
        _proceedInfo = proceedInfo;
        _proceed = proceed;
    }

    public object TargetObject => _invocation.InvocationTarget;

    public MethodInfo Method => _invocation.MethodInvocationTarget ?? _invocation.Method;

    public object?[] Arguments => _invocation.Arguments;

    public object? ReturnValue
    {
        get => _invocation.ReturnValue;
        set => _invocation.ReturnValue = value;
    }

    public async Task ProceedAsync()
    {
        await _proceed(_invocation, _proceedInfo);
    }
}

public class CastleAbpMethodInvocationAdapterWithReturnValue<TResult> : IAbpMethodInvocation
{
    private readonly IInvocation _invocation;
    private readonly IInvocationProceedInfo _proceedInfo;
    private readonly Func<IInvocation, IInvocationProceedInfo, Task<TResult>> _proceed;

    public CastleAbpMethodInvocationAdapterWithReturnValue(
        IInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
    {
        _invocation = invocation;
        _proceedInfo = proceedInfo;
        _proceed = proceed;
    }

    public object TargetObject => _invocation.InvocationTarget;

    public MethodInfo Method => _invocation.MethodInvocationTarget ?? _invocation.Method;

    public object?[] Arguments => _invocation.Arguments;

    public object? ReturnValue
    {
        get => _invocation.ReturnValue;
        set => _invocation.ReturnValue = value;
    }

    public async Task ProceedAsync()
    {
        ReturnValue = await _proceed(_invocation, _proceedInfo);
    }
}
