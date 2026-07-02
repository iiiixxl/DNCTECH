using Authorization_Extend.Models;
using Authorization_Extend.Permissions;
using Authorization_Extend.Permissions.Authorization;
using Authorization_Extend.Permissions.Reports;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.Controllers;

/// <summary>
/// 【动态权限】报表示例：新增报表时注册动态权限，访问时用 Reports.View.{code} 校验。
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class ReportController : ControllerBase
{
    private readonly IReportStore _reportStore;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IDynamicPermissionRegistrar _dynamicPermissionRegistrar;
    private readonly IPermissionDefinitionManager _definitionManager;

    public ReportController(
        IReportStore reportStore,
        IPermissionChecker permissionChecker,
        IDynamicPermissionRegistrar dynamicPermissionRegistrar,
        IPermissionDefinitionManager definitionManager)
    {
        _reportStore = reportStore;
        _permissionChecker = permissionChecker;
        _dynamicPermissionRegistrar = dynamicPermissionRegistrar;
        _definitionManager = definitionManager;
    }

    [HttpGet("permissions")]
    [RequirePermission(PermissionNames.Reports.Create)]
    public IActionResult ListPermissionDefinitions()
    {
        var items = _definitionManager.GetAll()
            .Select(p => new { p.Name, p.DisplayName, p.GroupName, p.IsDynamic });

        return Ok(items);
    }

    [HttpPost("configs")]
    [RequirePermission(PermissionNames.Reports.Create)]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request)
    {
        var report = new ReportRecord { Code = request.Code, Name = request.Name };

        await _reportStore.AddAsync(report);
        _dynamicPermissionRegistrar.RegisterReportPermission(report);

        return Ok(new
        {
            message = "报表已创建，动态权限已注册",
            permission = PermissionNames.Reports.GetViewPermission(report.Code),
            hint = "请在权限管理接口中把该权限授予角色"
        });
    }

    /// <summary>
    /// 运行时拼接权限名 Reports.View.{reportCode}，统一走 IPermissionChecker。
    /// </summary>
    [HttpGet("{reportCode}/data")]
    public async Task<IActionResult> GetReportData(string reportCode)
    {
        var report = await _reportStore.FindAsync(reportCode);
        if (report is null)
        {
            return NotFound(new { message = $"报表 {reportCode} 不存在" });
        }

        var permissionName = PermissionNames.Reports.GetViewPermission(reportCode);
        if (!await _permissionChecker.IsGrantedAsync(User, permissionName))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "无权查看该报表",
                requiredPermission = permissionName
            });
        }

        return Ok(new
        {
            report.Code,
            report.Name,
            data = new[] { new { column = "示例列", value = 100 } },
            message = $"已通过权限 {permissionName} 校验"
        });
    }
}
