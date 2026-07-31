using AbpDynamicProxy_Demo.AdapterConcept.Adapters;
using AbpDynamicProxy_Demo.AdapterConcept.Demo;
using AbpDynamicProxy_Demo.AdapterConcept.Foreign;
using Microsoft.AspNetCore.Mvc;

namespace AbpDynamicProxy_Demo.AdapterConcept;

/// <summary>
/// 用 HTTP 跑一遍「第三方代理 → 双适配器 → 我们的拦截器 → 业务」。
/// 重点看返回的 trace 步骤，对照 ABP 的 Castle 双适配器。
/// </summary>
[ApiController]
[Route("api/adapter-concept")]
public class AdapterConceptController : ControllerBase
{
    /// <summary>
    /// 成功路径：带 Create 权限。
    /// GET /api/adapter-concept/run?permissions=Demo.Users,Demo.Users.Create
    /// </summary>
    [HttpGet("run")]
    public IActionResult Run([FromQuery] string permissions = "Demo.Users,Demo.Users.Create")
    {
        var trace = new List<string>();
        var granted = permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // 1) 我们的拦截器（不认识 Foreign*）
        var appInterceptor = new PermissionCheckInterceptor(granted, trace);

        // 2) 适配器①：假装成第三方拦截器
        IForeignInterceptor foreignInterceptor = new ForeignToAppInterceptorAdapter(appInterceptor, trace);

        // 3) 第三方代理只认 IForeignInterceptor
        var realService = new OrderService();
        var proxy = new ForeignProxy(realService, foreignInterceptor);

        trace.Add("=== 开始：通过第三方代理调用 Create ===");
        object? result;
        try
        {
            result = proxy.Invoke("Create", "book-a");
            trace.Add($"=== 结束：成功，结果 = {result} ===");
            return Ok(new
            {
                success = true,
                result,
                permissions = granted,
                trace,
                mapping = new
                {
                    ForeignProxy = "类似 Castle 生成的 Proxy",
                    IForeignInterceptor = "类似 Castle 要求你实现的拦截器接口",
                    ForeignToAppInterceptorAdapter = "适配器① ≈ CastleAsyncAbpInterceptorAdapter",
                    ForeignToAppInvocationAdapter = "适配器② ≈ CastleAbpMethodInvocationAdapter",
                    IAppInterceptor = "类似 IAbpInterceptor（业务只依赖它）",
                    PermissionCheckInterceptor = "类似 AuthorizationInterceptor"
                }
            });
        }
        catch (Exception ex)
        {
            trace.Add($"=== 结束：失败 {ex.Message} ===");
            return StatusCode(403, new
            {
                success = false,
                error = ex.Message,
                permissions = granted,
                trace
            });
        }
    }

    /// <summary>
    /// 失败路径：只有 Default，没有 Create → 鉴权失败，业务不执行。
    /// GET /api/adapter-concept/deny
    /// </summary>
    [HttpGet("deny")]
    public IActionResult Deny()
    {
        return Run("Demo.Users");
    }
}
