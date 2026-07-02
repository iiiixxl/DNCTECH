using System.Security.Claims;
using Authentication_jwt_cookie.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication_jwt_cookie.Controllers;

/// <summary>
/// Cookie 认证：登录后由服务端 Set-Cookie，后续请求自动携带。
/// </summary>
/// <remarks>
/// 【模式一 · 默认 Cookie】
///   登出时 SignOutAsync 仅清除浏览器 Cookie，服务端无会话可删。
///
/// 【模式二 · 服务端会话 · 当前启用】
///   Cookie 仅携带 SessionId，完整会话由 MemoryTicketStore 保存；
///   登出时 SignOutAsync 同时删除服务端会话并清除 Cookie。
/// </remarks>
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Cookie 登录。演示账号：admin / 123456
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request.Username != "admin" || request.Password != "123456")
        {
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, "User")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return Ok(new { message = "登录成功，会话已保存在服务端" });
    }

    /// <summary>
    /// Cookie 登出，清除服务端会话与浏览器 Cookie。
    /// </summary>
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "已登出，服务端会话已删除" });
    }
}
