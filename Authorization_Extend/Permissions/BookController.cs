using Authorization_Extend.Models;
using Authorization_Extend.Permissions.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.Permissions;

/// <summary>
/// 【动态权限】仿 ABP：PermissionDefinitionProvider + GrantStore + 统一 Handler。
/// 每个 Action 标记权限名即可，无需在 Program 里逐个 AddPolicy。
/// </summary>
[ApiController]
[Route("api/books")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class BookController : ControllerBase
{
    private static readonly List<object> Books =
    [
        new { Id = 1, Title = "深入理解计算机系统" },
        new { Id = 2, Title = "CLR via C#" }
    ];

    [HttpGet]
    [RequirePermission(PermissionNames.Books.Default)]
    public IActionResult GetList() => Ok(new { approach = "dynamic-permission", data = Books });

    [HttpPost]
    [RequirePermission(PermissionNames.Books.Create)]
    public IActionResult Create([FromBody] CreateBookRequest request)
    {
        return Ok(new { approach = "dynamic-permission", message = $"已创建图书：{request.Title}" });
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionNames.Books.Update)]
    public IActionResult Update(int id, [FromBody] CreateBookRequest request)
    {
        return Ok(new { approach = "dynamic-permission", message = $"已更新图书 {id}：{request.Title}" });
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionNames.Books.Delete)]
    public IActionResult Delete(int id)
    {
        return Ok(new { approach = "dynamic-permission", message = $"已删除图书 {id}" });
    }
}
