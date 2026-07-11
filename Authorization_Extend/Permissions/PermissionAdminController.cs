using Authorization_Extend.Models;
using Authorization_Extend.Permissions.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.Permissions;

/// <summary>
/// 权限授予管理：模拟 ABP 权限管理页「给角色勾选权限」。
/// </summary>
[ApiController]
[Route("api/permission-admin")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class PermissionAdminController : ControllerBase
{
    private readonly IPermissionGrantStore _grantStore;
    private readonly IPermissionDefinitionManager _definitionManager;

    public PermissionAdminController(
        IPermissionGrantStore grantStore,
        IPermissionDefinitionManager definitionManager)
    {
        _grantStore = grantStore;
        _definitionManager = definitionManager;
    }

    [HttpGet("tree")]
    [RequirePermission(PermissionNames.Reports.Create)]
    public IActionResult GetPermissionTree()
    {
        var tree = _definitionManager.GetAll()
            .GroupBy(p => p.GroupName)
            .Select(g => new
            {
                group = g.Key,
                permissions = g.Select(p => new { p.Name, p.DisplayName, p.IsDynamic })
            });

        return Ok(tree);
    }

    [HttpGet("roles/{roleName}")]
    [RequirePermission(PermissionNames.Reports.Create)]
    public async Task<IActionResult> GetRolePermissions(string roleName)
    {
        var granted = await _grantStore.GetGrantedPermissionsAsync(roleName);
        return Ok(new { role = roleName, permissions = granted });
    }

    [HttpPost("roles/{roleName}/grant")]
    [RequirePermission(PermissionNames.Reports.Create)]
    public async Task<IActionResult> Grant(string roleName, [FromBody] GrantPermissionRequest request)
    {
        if (!_definitionManager.Exists(request.PermissionName))
        {
            return BadRequest(new { message = $"权限 {request.PermissionName} 未定义" });
        }

        await _grantStore.GrantAsync(roleName, request.PermissionName);
        return Ok(new { message = $"已授予 {roleName} → {request.PermissionName}" });
    }
}
