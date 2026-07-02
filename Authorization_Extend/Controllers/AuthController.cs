using System.Security.Claims;
using Authorization_Extend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.Controllers;

/// <summary>
/// Cookie 登录：admin → Admin 角色 + user-admin，user → User 角色 + user-normal。
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request.Username is not ("admin" or "user"))
        {
            return Unauthorized(new { message = "用户名或密码错误（可用 admin/user，密码 123456）" });
        }

        if (request.Password != "123456")
        {
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        var role = request.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "User";
        var userId = request.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) ? "user-admin" : "user-normal";

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
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

        return Ok(new { message = "登录成功", role, userId });
    }

    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "已登出" });
    }
}
