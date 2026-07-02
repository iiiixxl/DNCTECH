using Authorization_Extend.PolicyCodeAuthorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.Controllers;

/// <summary>
/// 【极简动态权限】策略名 = 权限编码，Provider 动态构建 Policy，Handler 按 userId 查库。
/// </summary>
/// <remarks>
/// 用法：[Authorize(Policy = "User.Delete")] 或 [RequirePermissionCode("User.Delete")]
/// 无需在 Program 中 AddPolicy，也无需 PermissionDefinitionProvider。
///
/// 与 ABP 风格对比：
/// - 更简单：3 个核心类（Requirement + Provider + Handler）+ 1 个查库服务
/// - 适合：权限直接绑定用户、无复杂权限树/UI 的场景
/// - 局限：无权限定义管理、无角色批量授权、无动态注册权限元数据
/// </remarks>
[ApiController]
[Route("api/policy-code")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class PolicyCodeUserController : ControllerBase
{
    [HttpGet]
    [RequirePermissionCode(PolicyCodePermissionNames.UserView)]
    public IActionResult GetUsers()
    {
        return Ok(new
        {
            approach = "policy-code-minimal",
            data = new[]
            {
                new { Id = 1, Name = "张三" },
                new { Id = 2, Name = "李四" }
            }
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PolicyCodePermissionNames.UserDelete)]
    public IActionResult DeleteUser(int id)
    {
        return Ok(new { approach = "policy-code-minimal", message = $"已删除用户 {id}" });
    }

    [HttpPost("orders")]
    [Authorize(Policy = PolicyCodePermissionNames.OrderCreate)]
    public IActionResult CreateOrder()
    {
        return Ok(new { approach = "policy-code-minimal", message = "订单已创建" });
    }
}
