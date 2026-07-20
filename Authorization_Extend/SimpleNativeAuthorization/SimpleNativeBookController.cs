using Authorization_Extend.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.SimpleNativeAuthorization;

/// <summary>
/// 【扩展原生 · 简化版】在原生 Role 授权基础上封装 RequireRoles 特性，无需 AddPolicy。
/// </summary>
/// <remarks>
/// 与 NativeBookController 对比：
/// - 不需要在 Program 中逐个 AddPolicy
/// - 直接在 Action 上写 [RequireRoles("Admin", "User")]
/// - 仍然基于 Role，不支持运行时动态授权、不支持权限授予管理
/// - 适合快速开发，但角色变更需改代码重新部署
/// </remarks>
[ApiController]
[Route("api/simple-native-books")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class SimpleNativeBookController : ControllerBase
{
    private static readonly List<object> Books =
    [
        new { Id = 1, Title = "代码整洁之道" },
        new { Id = 2, Title = "人月神话" }
    ];

    [HttpGet]
    [Authorize(Roles = "Admin,User")]
    public IActionResult GetList()
    {
        return Ok(new { approach = "simple-native-roles", data = Books });
    }

    [HttpPost]
    [RequireRoles("Admin")]
    public IActionResult Create([FromBody] CreateBookRequest request)
    {
        return Ok(new { approach = "simple-native-roles", message = $"已创建图书：{request.Title}" });
    }

    [HttpPut("{id:int}")]
    [RequireRoles("Admin")]
    public IActionResult Update(int id, [FromBody] CreateBookRequest request)
    {
        return Ok(new { approach = "simple-native-roles", message = $"已更新图书 {id}：{request.Title}" });
    }

    [HttpDelete("{id:int}")]
    [RequireRoles("Admin")]
    public IActionResult Delete(int id)
    {
        return Ok(new { approach = "simple-native-roles", message = $"已删除图书 {id}" });
    }
}
