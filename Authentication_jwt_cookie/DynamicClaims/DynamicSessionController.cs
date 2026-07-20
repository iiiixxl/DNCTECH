using System.Security.Claims;
using Authentication_jwt_cookie.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>
/// DynamicClaims Demo：登录 / 会话撤销 / 动态角色 / 访问。
/// </summary>
/// <remarks>
/// 对齐 ABP：
/// 1. 登录：SessionClaimsPrincipalContributor 写 session_id → Save 会话 → 签发 JWT
/// 2. 每请求：SessionDynamicClaimsContributor 校验会话；IdentityDynamicClaimsContributor 覆盖 Role
/// Token 不变，权威源一改，下次请求内存身份立刻变（角色→可能 403；会话删→401）
/// </remarks>
[ApiController]
[Route("dynamic-claims")]
[Authorize]
public class DynamicSessionController : ControllerBase
{
    private readonly UserSessionStore _sessions;
    private readonly DemoUserClaimStore _claimStore;
    private readonly SessionJwtTokenService _tokenService;
    private readonly LoginClaimsPrincipalFactory _principalFactory;

    public DynamicSessionController(
        UserSessionStore sessions,
        DemoUserClaimStore claimStore,
        SessionJwtTokenService tokenService,
        LoginClaimsPrincipalFactory principalFactory)
    {
        _sessions = sessions;
        _claimStore = claimStore;
        _tokenService = tokenService;
        _principalFactory = principalFactory;
    }

    /// <summary>登录。账号 admin / 123456（初始角色 Admin + User）</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request.Username != "admin" || request.Password != "123456")
        {
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        var principal = await _principalFactory.CreateAsync(request.Username);
        var sessionId = principal.FindFirst(AppClaimTypes.SessionId)!.Value;

        _sessions.Save(sessionId, request.Username);

        var (token, expiresUtc) = _tokenService.GenerateToken(principal);

        return Ok(new
        {
            message = "登录成功。Token 含 session_id；角色以服务端权威源为准，可热更新",
            token,
            sessionId,
            rolesInStore = _claimStore.GetRoles(request.Username),
            tokenType = "Bearer",
            expiresUtc
        });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            name = User.Identity?.Name,
            sessionId = User.FindFirst(AppClaimTypes.SessionId)?.Value,
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
            claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
        });
    }

    /// <summary>仅 Admin：用来验证「改角色后 Token 不变，立刻 403」。</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly() =>
        Ok(new { message = "你当前仍是 Admin", user = User.Identity?.Name });

    /// <summary>
    /// 热更新角色权威源（演示用）。无需重新登录，下次请求 IdentityDynamicClaimsContributor 会覆盖 Token 旧 Role。
    /// </summary>
    [AllowAnonymous]
    [HttpPut("users/{username}/roles")]
    public IActionResult SetRoles(string username, [FromBody] string[] roles)
    {
        _claimStore.SetRoles(username, roles);
        return Ok(new
        {
            message = $"已更新 {username} 的角色，Token 不用换，下次请求立即生效",
            username,
            roles
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var sessionId = User.FindFirst(AppClaimTypes.SessionId)?.Value;
        if (string.IsNullOrEmpty(sessionId))
        {
            return BadRequest(new { message = "Token 缺少 session_id" });
        }

        _sessions.Revoke(sessionId);
        return Ok(new { message = "会话已撤销，后续请求将返回 401", sessionId });
    }

    [AllowAnonymous]
    [HttpPost("sessions/{sessionId}/revoke")]
    public IActionResult RevokeSession(string sessionId)
    {
        _sessions.Revoke(sessionId);
        return Ok(new { message = "会话已吊销", sessionId });
    }

    [AllowAnonymous]
    [HttpPost("users/{username}/revoke-all")]
    public IActionResult RevokeAll(string username)
    {
        var count = _sessions.RevokeAll(username);
        return Ok(new { message = $"已吊销 {count} 个会话", username, count });
    }

    [AllowAnonymous]
    [HttpGet("users/{username}/sessions")]
    public IActionResult ListSessions(string username) =>
        Ok(_sessions.GetByUser(username).Select(s => new
        {
            s.SessionId,
            s.Username,
            s.CreatedAt,
            s.LastAccessed
        }));
}
