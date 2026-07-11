using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Authorization_Extend.AuthResultHandler;

/// <summary>
/// 自定义授权结果处理器：接管授权中间件「拒绝」时的响应。
/// </summary>
/// <remarks>
/// 解决的核心问题：框架默认对「未认证」和「无权限」都只吐一个空 body 的状态码
/// （401 / 403），前端拿不到结构化信息，既分不清是「没登录」还是「登录了但没权限」，
/// 也拿不到「差哪个权限 / 差哪个租户」这类可用于精准提示的细节。
///
/// 授权中间件在跑完策略后，会把结果交给容器里唯一的 IAuthorizationMiddlewareResultHandler。
/// 我们替换掉默认实现，就能统一改写拒绝时的响应体：
/// - Challenged（未认证）：返回结构化 401 JSON；若是浏览器页面请求则跳登录页。
/// - Forbidden（已认证但无权限）：返回结构化 403 JSON，并带上所需租户等排障信息。
/// - 通过：原样交回默认处理器，继续走后续管道（走到 Controller）。
/// </remarks>
public class CustomAuthResultHandler : IAuthorizationMiddlewareResultHandler
{
    // 保留一个官方默认处理器，授权「通过」时把控制权原样交回去，避免自己漏处理管道细节
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // 情况一：未认证（没登录 / Cookie 失效）。框架里叫 Challenged。
        if (authorizeResult.Challenged)
        {
            // 特殊场景跳转：根据请求来源区别对待
            // - 浏览器直接打开页面（Accept: text/html）→ 302 跳登录页，体验更顺
            // - 前后端分离的 API 调用（Ajax / fetch）→ 返回 401 JSON，交给前端拦截器处理
            if (WantsHtml(context))
            {
                var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect($"/login?returnUrl={returnUrl}");
                return;
            }

            await WriteJsonAsync(context, StatusCodes.Status401Unauthorized, new
            {
                code = 401,
                message = "未登录或登录已过期，请重新登录"
            });
            return;
        }

        // 情况二：已认证但权限不足。框架里叫 Forbidden。
        if (authorizeResult.Forbidden)
        {
            // 40301 是业务自定义错误码：前两位沿用 HTTP 403，后面用于细分具体拒绝原因，
            // 前端可据此弹不同的提示。同时带上「所需租户」帮助定位越权/串租户问题。
            await WriteJsonAsync(context, StatusCodes.Status403Forbidden, new
            {
                code = 40301,
                message = "无门店操作权限",
                requiredTenantId = context.User.FindFirst("tenant_id")?.Value
            });
            return;
        }

        // 情况三：授权通过，交回默认处理器继续执行后续中间件与 Controller
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    /// <summary>判断是否为浏览器页面请求（据 Accept 头），用于「未登录」时决定跳页还是回 JSON。</summary>
    private static bool WantsHtml(HttpContext context)
    {
        var accept = context.Request.Headers.Accept.ToString();
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static Task WriteJsonAsync(HttpContext context, int statusCode, object payload)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
