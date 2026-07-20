using System.Security.Claims;

namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>
/// 登录组 Claim 时写入 <c>session_id</c>。
/// 对应 ABP <c>IdentitySessionClaimsPrincipalContributor</c>。
/// </summary>
public class SessionClaimsPrincipalContributor : IClaimsPrincipalContributor
{
    public Task ContributeAsync(ClaimsPrincipalContributorContext context)
    {
        var identity = context.ClaimsPrincipal.Identities.FirstOrDefault();
        if (identity is null)
        {
            return Task.CompletedTask;
        }

        if (identity.FindFirst(AppClaimTypes.SessionId) is null)
        {
            identity.AddClaim(new Claim(AppClaimTypes.SessionId, Guid.NewGuid().ToString("N")));
        }

        return Task.CompletedTask;
    }
}
