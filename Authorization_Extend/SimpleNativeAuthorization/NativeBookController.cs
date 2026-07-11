using Authorization_Extend.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.SimpleNativeAuthorization;

/// <summary>
/// 【原生授权】ASP.NET Core 标准做法：Program 中 AddPolicy + [Authorize(Policy = "...")]。
/// </summary>
/// <remarks>
/// 特点：
/// - 每个操作需在 Program 中显式注册 Policy（见 AddNativeBookPolicies）
/// - Policy 直接绑定 Role，无法运行时动态调整
/// - 适合权限固定、角色较少的简单场景
/// </remarks>
[ApiController]
[Route("api/native-books")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class NativeBookController : ControllerBase
{
    private static readonly List<object> Books =
    [
        new { Id = 1, Title = "设计模式" },
        new { Id = 2, Title = "重构" }
    ];

    [HttpGet]
    [Authorize(Policy = NativeBookPolicyNames.View)]
    public IActionResult GetList()
    {
        return Ok(new { approach = "native-policy", data = Books });
    }

    [HttpPost]
    [Authorize(Policy = NativeBookPolicyNames.Create)]
    public IActionResult Create([FromBody] CreateBookRequest request)
    {
        return Ok(new { approach = "native-policy", message = $"已创建图书：{request.Title}" });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = NativeBookPolicyNames.Update)]
    public IActionResult Update(int id, [FromBody] CreateBookRequest request)
    {
        return Ok(new { approach = "native-policy", message = $"已更新图书 {id}：{request.Title}" });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = NativeBookPolicyNames.Delete)]
    public IActionResult Delete(int id)
    {
        return Ok(new { approach = "native-policy", message = $"已删除图书 {id}" });
    }
}
