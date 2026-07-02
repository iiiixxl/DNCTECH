using Authorization_Extend.PolicyCodeAuthorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.Controllers;

/// <summary>
/// 查看当前登录用户拥有的 PolicyCode 权限（调试用）。
/// </summary>
[ApiController]
[Route("api/policy-code/me")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class PolicyCodeMeController : ControllerBase
{
    private readonly IUserPermissionService _permissionService;

    public PolicyCodeMeController(IUserPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "缺少 NameIdentifier Claim" });
        }

        var permissions = await _permissionService.GetUserPermissionsAsync(userId);
        return Ok(new { userId, permissions });
    }
}
