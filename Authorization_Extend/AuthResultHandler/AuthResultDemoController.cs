using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.AuthResultHandler;

/// <summary>
/// 【自定义授权结果处理器】演示：观察拒绝时返回的结构化 JSON。
/// </summary>
/// <remarks>
/// 这里刻意造出「未认证」和「无权限」两种拒绝，用来对比默认响应与自定义响应的差别：
/// - not-login   ：只要求登录。未登录访问 → Challenged → 自定义 401 JSON（或跳登录页）。
/// - admin-only  ：要求 Admin 角色。用 user 登录后访问 → Forbidden → 自定义 403 JSON。
///
/// 注意：CustomAuthResultHandler 是全局生效的，项目里所有被授权拒绝的请求都会走它，
/// 这里只是挑两个最典型的入口方便演示。
/// </remarks>
[ApiController]
[Route("api/auth-result")]
public class AuthResultDemoController : ControllerBase
{
    /// <summary>只要求「已登录」。未登录访问会被 Challenged，返回自定义 401。</summary>
    [HttpGet("need-login")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public IActionResult NeedLogin()
    {
        return Ok(new { approach = "custom-auth-result", message = "你已登录，正常拿到数据" });
    }

    /// <summary>要求「Admin 角色」。用 user（非 Admin）登录访问会被 Forbidden，返回自定义 403。</summary>
    [HttpGet("admin-only")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new { approach = "custom-auth-result", message = "你是 Admin，允许操作" });
    }
}
