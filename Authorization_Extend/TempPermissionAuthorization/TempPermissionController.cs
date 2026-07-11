using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.TempPermissionAuthorization;

/// <summary>
/// 临时权限授予 Demo 控制器。
/// 实现了什么：
/// 1. 主管（Admin）给下属发放带时效的临时权限（写入授权库，等价于签发时效性令牌声明）；
/// 2. 下属请求时由 TempPermissionClaimsTransformer 注入未过期的 TempPermission / TempValidUntil；
/// 3. 代审接口走 Temp.ExpenseApprove 策略，TempPermissionHandler 校验时效，过期自动 403。
/// </summary>
[ApiController]
[Route("api/temp-permission")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class TempPermissionController : ControllerBase
{
    private readonly ITempPermissionStore _store;

    public TempPermissionController(ITempPermissionStore store)
    {
        _store = store;
    }

    /// <summary>
    /// 查看当前用户身上的临时权限声明（转换器注入后的结果）。
    /// 用于对照：授权前为空；主管授权后出现 TempPermission + TempValidUntil；过期后消失。
    /// </summary>
    [HttpGet("me")]
    public IActionResult GetMyTempClaims()
    {
        var tempClaims = User.Claims
            .Where(c => c.Type is TempClaimTypes.Permission
                or TempClaimTypes.ValidUntil
                or TempClaimTypes.Enriched)
            .Select(c => new { c.Type, c.Value })
            .ToList();

        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            tempClaims,
            hint = "先由 Admin 调用 POST /grant，再用 user 登录后刷新本接口，即可看到注入的临时声明"
        });
    }

    /// <summary>
    /// 主管授权：给指定下属发放临时权限（默认 2 小时）。
    /// 业务场景：财务主管临时授权下属代审报销单。
    /// 这里写入内存授权库；真实项目可同时签发带同样 Claim 的短时 JWT。
    /// </summary>
    [HttpPost("grant")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Grant([FromBody] GrantTempPermissionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GranteeUserId))
        {
            return BadRequest(new { message = "granteeUserId 不能为空（演示可用 user-normal）" });
        }

        var permission = string.IsNullOrWhiteSpace(request.Permission)
            ? TempPermissionNames.ExpenseApprove
            : request.Permission;

        // durationMinutes 默认 120（2 小时）；传很小的值（如 0.05 分钟）可快速演示过期
        var minutes = request.DurationMinutes <= 0 ? 120 : request.DurationMinutes;
        var validUntil = DateTimeOffset.UtcNow.AddMinutes(minutes);
        var grantedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        var grant = new TempPermissionGrant
        {
            GranteeUserId = request.GranteeUserId,
            Permission = permission,
            ValidUntil = validUntil,
            GrantedByUserId = grantedBy
        };

        await _store.GrantAsync(grant);

        // 展示「若签发 Token，会带上哪些声明」——对应需求里的 GenerateToken(claims)
        var tokenClaimsPreview = new[]
        {
            new { Type = TempClaimTypes.Permission, Value = permission },
            new
            {
                Type = TempClaimTypes.ValidUntil,
                Value = TempPermissionClaimsTransformer.FormatValidUntil(permission, validUntil)
            }
        };

        return Ok(new
        {
            message = $"已授予 {request.GranteeUserId} 临时权限 {permission}，有效至 {validUntil:O}",
            grant,
            tokenClaimsPreview,
            nextStep = "用下属账号重新请求即可；声明转换会自动注入未过期的临时 Claim"
        });
    }

    /// <summary>
    /// 主动撤销临时权限（主管收回授权，立即生效，无需等时效到期）。
    /// </summary>
    [HttpPost("revoke")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTempPermissionRequest request)
    {
        var permission = string.IsNullOrWhiteSpace(request.Permission)
            ? TempPermissionNames.ExpenseApprove
            : request.Permission;

        await _store.RevokeAsync(request.GranteeUserId, permission);
        return Ok(new { message = $"已撤销 {request.GranteeUserId} 的临时权限 {permission}" });
    }

    /// <summary>
    /// 查看某用户的授权记录（含已过期），方便对照「库里有记录但已过期 → 转换器不注入」。
    /// </summary>
    [HttpGet("grants/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListGrants(string userId)
    {
        var all = await _store.GetAllGrantsAsync(userId);
        return Ok(all.Select(g => new
        {
            g.GranteeUserId,
            g.Permission,
            g.ValidUntil,
            g.GrantedByUserId,
            g.GrantedAt,
            g.IsActive
        }));
    }

    /// <summary>
    /// 代审报销单：需要未过期的 expense.approve 临时权限。
    /// 固定角色做不到「只开 2 小时」；本接口靠 TempPermissionHandler 卡时效。
    /// </summary>
    [HttpPost("expenses/{id:int}/approve")]
    [Authorize(Policy = TempPermissionNames.ExpenseApprovePolicy)]
    public IActionResult ApproveExpense(int id)
    {
        var until = User.FindAll(TempClaimTypes.ValidUntil)
            .Select(c => c.Value)
            .FirstOrDefault(v =>
                v.StartsWith(TempPermissionNames.ExpenseApprove + "|", StringComparison.OrdinalIgnoreCase));

        return Ok(new
        {
            message = $"报销单 {id} 已代审通过",
            approvedBy = User.FindFirstValue(ClaimTypes.NameIdentifier),
            tempValidUntil = until,
            note = "本操作依赖临时权限，时效到期或被撤销后将返回 403"
        });
    }
}

/// <summary>主管授权请求体。</summary>
public class GrantTempPermissionRequest
{
    /// <summary>被授权人 userId，演示填 user-normal。</summary>
    public string GranteeUserId { get; set; } = "user-normal";

    /// <summary>权限编码，默认 expense.approve。</summary>
    public string? Permission { get; set; }

    /// <summary>有效分钟数，默认 120（2 小时）。演示过期可传 0.05（约 3 秒）。</summary>
    public double DurationMinutes { get; set; } = 120;
}

/// <summary>撤销临时权限请求体。</summary>
public class RevokeTempPermissionRequest
{
    public string GranteeUserId { get; set; } = "user-normal";
    public string? Permission { get; set; }
}
