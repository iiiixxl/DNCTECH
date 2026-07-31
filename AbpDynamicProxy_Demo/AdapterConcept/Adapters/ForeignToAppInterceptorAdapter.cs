using AbpDynamicProxy_Demo.AdapterConcept.App;
using AbpDynamicProxy_Demo.AdapterConcept.Foreign;

namespace AbpDynamicProxy_Demo.AdapterConcept.Adapters;

/// <summary>
/// ★ 适配器①：拦截器形态适配
/// 实现第三方要求的 IForeignInterceptor，内部转调我们的 IAppInterceptor。
/// 对应 ABP: CastleAsyncAbpInterceptorAdapter
/// </summary>
public class ForeignToAppInterceptorAdapter : IForeignInterceptor
{
    private readonly IAppInterceptor _appInterceptor;
    private readonly List<string> _trace;

    public ForeignToAppInterceptorAdapter(IAppInterceptor appInterceptor, List<string> trace)
    {
        _appInterceptor = appInterceptor;
        _trace = trace;
    }

    public void Intercept(ForeignInvocation foreignInvocation)
    {
        _trace.Add("适配器① ForeignToAppInterceptorAdapter.Intercept：收到第三方回调");

        // 先用适配器②把上下文翻译成我们的模型
        var appInvocation = new ForeignToAppInvocationAdapter(foreignInvocation);
        _trace.Add("适配器② ForeignToAppInvocationAdapter：已包装成 IAppInvocation");

        // 再调用我们自己的拦截器（业务只认这一套）
        _appInterceptor.InterceptAsync(appInvocation).GetAwaiter().GetResult();

        _trace.Add("适配器①：我们的 IAppInterceptor 已执行完毕");
    }
}
