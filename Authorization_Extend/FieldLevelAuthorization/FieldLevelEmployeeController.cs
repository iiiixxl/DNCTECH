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
/// 1. 基于资源的动态授权 —— AuthorizeAsync(User, EmployeeDto, FieldAccessRequirement)。
/// 2. 自定义授权处理器 —— FieldAccessHandler 读 FieldPermission Claim 决定字段是否可见。
/// 3. 在组装 DTO 时动态过滤字段，避免「为每个字段组合造角色」导致角色爆炸，
///    也避免在业务层写死 if (User.IsInRole("HR")) 与权限层耦合。
///
/// 演示账号（登录后 Cookie 里已写入 FieldPermission Claim）：
/// - admin（user-admin）：BaseSalary + Bonus + SocialSecurity → 看得到全部敏感列
/// - user（user-normal）：仅 BaseSalary → 只能看到基本工资，奖金/社保为 null
/// </remarks>
[ApiController]
[Route("api/field-employees")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class FieldLevelEmployeeController : ControllerBase
{
    private readonly EmployeeService _employeeService;
    private readonly IEmployeeStore _employeeStore;

    public FieldLevelEmployeeController(EmployeeService employeeService, IEmployeeStore employeeStore)
    {
        _employeeService = employeeService;
        _employeeStore = employeeStore;
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

        var dto = await _employeeService.GetProfileAsync(User, userId);
        if (dto is null)
        {
            return NotFound(new { message = $"未找到员工档案 {userId}" });
        }

        return Ok(new
        {
            approach = "field-level",
            message = "已按 FieldPermission Claim 动态过滤敏感字段",
            data = dto
        });
    }

    /// <summary>
    /// 按 userId 查看任意员工薪资（HR 场景：专员查看下属档案）。
    /// 注意：本接口演示的是「字段级」过滤，未再叠加「只能看自己」的资源归属校验；
    /// 真实 HR 系统通常两者都要：先校验能不能看这个人，再按字段裁剪。
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetProfile(string userId)
    {
        var dto = await _employeeService.GetProfileAsync(User, userId);
        if (dto is null)
        {
            return NotFound(new { message = $"未找到员工档案 {userId}" });
        }

        return Ok(new
        {
            approach = "field-level",
            message = "同一资源、不同调用者看到的字段集合不同",
            data = dto
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
    /// 列出全部员工 Id/姓名（不做字段过滤，仅方便知道有哪些 userId 可查）。
    /// </summary>
    [HttpGet]
    public IActionResult ListEmployees()
    {
        var list = _employeeStore.All()
            .Select(e => new { e.UserId, e.Name });

        return Ok(new { approach = "field-level", data = list });
    }
}
