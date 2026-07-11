using System.Security.Claims;
using Authorization_Extend.FieldLevelAuthorization;
using Authorization_Extend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.Controllers;

/// <summary>
/// Cookie 登录：admin → Admin 角色 + user-admin，user → User 角色 + user-normal。
/// 同时写入 FieldPermission Claim，供「字段级动态授权」按字段裁剪薪资响应。
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

        var isAdmin = request.Username.Equals("admin", StringComparison.OrdinalIgnoreCase);
        var role = isAdmin ? "Admin" : "User";
        var userId = isAdmin ? "user-admin" : "user-normal";
        // 租户 Claim：供「基于资源的动态授权」做多租户隔离演示（admin→tenant-a，user→tenant-b）
        var tenantId = isAdmin ? "tenant-a" : "tenant-b";

        // 字段权限 Claim：一条 Claim = 一个可访问字段（避免为字段组合造角色）
        // admin(HR) → 基本工资 + 奖金 + 社保；user(员工) → 仅基本工资
        var fieldPermissions = isAdmin
            ? new[] { FieldNames.BaseSalary, FieldNames.Bonus, FieldNames.SocialSecurity }
            : new[] { FieldNames.BaseSalary };

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, request.Username),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
            new("tenant_id", tenantId)
        };

        foreach (var field in fieldPermissions)
        {
            claims.Add(new Claim(FieldClaimTypes.FieldPermission, field));
        }

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

        return Ok(new { message = "登录成功", role, userId, fieldPermissions });
    }

    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "已登出" });
    }
}
