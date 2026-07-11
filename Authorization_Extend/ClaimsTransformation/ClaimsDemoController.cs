using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.ClaimsTransformation;

/// <summary>
/// 【声明转换】演示：登录 Token 里只有粗粒度 role，进入接口前由 IClaimsTransformation
/// 从业务库补上「部门 / 细分角色 / 权限点」，让授权能基于这些丰富声明来做。
/// </summary>
/// <remarks>
/// 关键点：下面 finance-report / finance-approve 依赖的 FinanceAdmin 角色、finance.approve 权限，
/// 原始登录 Cookie 里都没有，全是 PermissionClaimsTransformer 在每次请求时动态补进去的。
/// 这样「财务管理员」和「内容管理员」即便原始 role 都是 Admin，也能被清晰区分开。
/// </remarks>
[ApiController]
[Route("api/claims-demo")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class ClaimsDemoController : ControllerBase
{
    /// <summary>看看当前用户「转换后」到底有哪些声明（调试/对照用）。</summary>
    [HttpGet("me")]
    public IActionResult GetMyClaims()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(new { approach = "claims-transformation", claims });
    }

    /// <summary>财务报表：需要细分角色 FinanceAdmin（转换补出来的，非原始 Token）。</summary>
    [HttpGet("finance-report")]
    [Authorize(Roles = "FinanceAdmin")]
    public IActionResult GetFinanceReport()
    {
        return Ok(new { approach = "claims-transformation", data = "财务报表数据（仅 FinanceAdmin 可见）" });
    }

    /// <summary>财务审批：需要 finance.approve 权限声明（转换补出来的）。</summary>
    [HttpPost("finance-approve")]
    [Authorize(Policy = ClaimsPermissionPolicyNames.FinanceApprove)]
    public IActionResult Approve()
    {
        return Ok(new { approach = "claims-transformation", message = "审批通过（需 finance.approve 权限声明）" });
    }

    /// <summary>编辑文章：需要细分角色 ContentEditor（内容管理员专属）。</summary>
    [HttpPut("articles/{id:int}")]
    [Authorize(Roles = "ContentEditor")]
    public IActionResult EditArticle(int id)
    {
        return Ok(new { approach = "claims-transformation", message = $"已编辑文章 {id}（仅 ContentEditor 可操作）" });
    }
}
