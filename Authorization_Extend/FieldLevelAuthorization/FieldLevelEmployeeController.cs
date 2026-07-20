using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>
/// 【字段级动态授权】演示控制器：同一份员工薪资资源，按字段权限差异化返回。
/// </summary>
/// <remarks>
/// 实现了什么：
/// 1. 基于资源的动态授权 —— 先校验 Employee 的行级访问，再校验 EmployeeDto 的字段可见性。
/// 2. 自定义授权处理器 —— FieldAccessHandler 读 FieldPermission Claim 决定字段是否可见。
/// 3. 在组装 DTO 时动态过滤字段，避免「为每个字段组合造角色」导致角色爆炸，
///    也避免在业务层写死 if (User.IsInRole("HR")) 与权限层耦合。
///
/// 演示账号（登录后 Cookie 里已写入 FieldPermission Claim）：
/// - admin（user-admin）：BaseSalary + Bonus + SocialSecurity → 看得到全部敏感列
/// - user（user-normal）：仅 BaseSalary → 只能看到基本工资，奖金/社保不写入响应
/// </remarks>
[ApiController]
[Route("api/field-employees")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class FieldLevelEmployeeController : ControllerBase
{
    private readonly EmployeeService _employeeService;
    private readonly IEmployeeStore _employeeStore;
    private readonly IAuthorizationService _authorizationService;

    public FieldLevelEmployeeController(
        EmployeeService employeeService,
        IEmployeeStore employeeStore,
        IAuthorizationService authorizationService)
    {
        _employeeService = employeeService;
        _employeeStore = employeeStore;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// 查看当前登录用户自己的薪资档案（字段按权限裁剪）。
    /// user → 只有 Name + BaseSalary；admin → 含 Bonus、SocialSecurity。
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "未登录或缺少用户标识" });
        }

        var result = await GetAuthorizedProfileAsync(userId);
        if (result.Result is not null)
        {
            return result.Result;
        }

        return Ok(new
        {
            approach = "field-level",
            message = "已按 FieldPermission Claim 动态过滤敏感字段",
            data = result.Dto
        });
    }

    /// <summary>
    /// 按 userId 查看员工薪资：先做档案行级授权，再按字段权限裁剪。
    /// 本人可查看自己的档案；Admin（演示中的 HR）可查看任意员工档案。
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetProfile(string userId)
    {
        var result = await GetAuthorizedProfileAsync(userId);
        if (result.Result is not null)
        {
            return result.Result;
        }

        return Ok(new
        {
            approach = "field-level",
            message = "已通过档案访问校验，并按 FieldPermission Claim 动态过滤敏感字段",
            data = result.Dto
        });
    }

    /// <summary>
    /// 列出当前用户持有的字段权限 Claim，便于联调对照。
    /// </summary>
    [HttpGet("me/field-permissions")]
    public IActionResult GetMyFieldPermissions()
    {
        var fields = User.Claims
            .Where(c => c.Type == FieldClaimTypes.FieldPermission)
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            approach = "field-level",
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            fieldPermissions = fields
        });
    }

    /// <summary>
    /// 列出全部员工 Id/姓名（仅 Admin，避免向普通员工暴露可枚举的档案标识）。
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public IActionResult ListEmployees()
    {
        var list = _employeeStore.All()
            .Select(e => new { e.UserId, e.Name });

        return Ok(new { approach = "field-level", data = list });
    }

    private async Task<(EmployeeDto? Dto, IActionResult? Result)> GetAuthorizedProfileAsync(string userId)
    {
        var employee = _employeeStore.Find(userId);
        if (employee is null)
        {
            return (null, NotFound(new { message = $"未找到员工档案 {userId}" }));
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(
            User,
            employee,
            new EmployeeProfileAccessRequirement());

        if (!authorizationResult.Succeeded)
        {
            return (null, Forbid());
        }

        var dto = await _employeeService.GetProfileAsync(User, userId);
        return (dto, null);
    }
}
