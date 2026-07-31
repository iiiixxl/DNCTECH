using System.Reflection;

namespace AbpDynamicProxy_Demo.DynamicProxy;

/// <summary>
/// 对应 ABP: Volo.Abp.DynamicProxy.IAbpMethodInvocation
/// 封装「当前这次方法调用」的上下文。
/// </summary>
public interface IAbpMethodInvocation
{
    object TargetObject { get; }

    MethodInfo Method { get; }

    object?[] Arguments { get; }

    object? ReturnValue { get; set; }

    Task ProceedAsync();
}
