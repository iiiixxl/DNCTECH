namespace AbpDynamicProxy_Demo.AdapterConcept.Demo;

/// <summary>
/// 真实业务对象（类似 UserAppService）。第三方代理会调到这里。
/// </summary>
public class OrderService
{
    public string GetList()
    {
        return "order-1,order-2";
    }

    public string Create(string name)
    {
        return $"created:{name}";
    }
}
