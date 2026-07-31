namespace AbpDynamicProxy_Demo.AdapterConcept.Foreign;

/// <summary>
/// 模拟第三方「代理」：调用业务前先调拦截器。
/// 真实世界里这是 Castle 生成的 Proxy；这里手写一个最小版本方便理解。
/// </summary>
public class ForeignProxy
{
    private readonly object _target;
    private readonly IForeignInterceptor _interceptor;

    public ForeignProxy(object target, IForeignInterceptor interceptor)
    {
        _target = target;
        _interceptor = interceptor;
    }

    public object? Invoke(string methodName, params object[] args)
    {
        ForeignInvocation? invocation = null;
        invocation = new ForeignInvocation
        {
            MethodName = methodName,
            Args = args,
            Continue = () =>
            {
                // 真正调用目标对象方法（简化：用反射）
                var method = _target.GetType().GetMethod(methodName)
                    ?? throw new InvalidOperationException($"Method not found: {methodName}");
                invocation!.Result = method.Invoke(_target, args);
            }
        };

        // 第三方只认 IForeignInterceptor
        _interceptor.Intercept(invocation);
        return invocation.Result;
    }
}
