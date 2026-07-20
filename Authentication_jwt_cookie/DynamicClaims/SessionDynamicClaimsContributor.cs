using System.Security.Claims;

namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>
/// 校验 JWT 中 <c>session_id</c> 是否仍在服务端。
/// 对应 ABP <c>IdentitySessionDynamicClaimsPrincipalContributor</c>。
/// </summary>
public class SessionDynamicClaimsContributor : IDynamicClaimsPrincipalContributor
{
    private readonly UserSessionStore _sessions;
    private readonly ILogger<SessionDynamicClaimsContributor> _logger;

    public SessionDynamicClaimsContributor(
        UserSessionStore sessions,
        ILogger<SessionDynamicClaimsContributor> logger)
    {
        _sessions = sessions;
        _logger = logger;
    }

    public Task ContributeAsync(DynamicClaimsContributeContext context)
    {
        var identity = context.Principal.Identities.FirstOrDefault();
        if (identity is null)
        {
            return Task.CompletedTask;
        }

        // 无 session_id = 普通 JWT Demo Token，不参与本校验
        var sessionId = identity.FindFirst(AppClaimTypes.SessionId)?.Value;
        if (sessionId is null)
        {
            return Task.CompletedTask;
        }

        if (!_sessions.IsValid(sessionId))
        {
            _logger.LogWarning("SessionId({SessionId}) invalid, force logout.", sessionId);
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return Task.CompletedTask;
    }
}
