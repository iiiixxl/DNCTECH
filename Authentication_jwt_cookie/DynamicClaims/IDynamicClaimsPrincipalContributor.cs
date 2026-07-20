using System.Security.Claims;

namespace Authentication_jwt_cookie.DynamicClaims;

public class DynamicClaimsContributeContext
{
    public ClaimsPrincipal Principal { get; set; }

    public DynamicClaimsContributeContext(ClaimsPrincipal principal) => Principal = principal;
}

/// <summary>对应 ABP <c>IAbpDynamicClaimsPrincipalContributor</c>。</summary>
public interface IDynamicClaimsPrincipalContributor
{
    Task ContributeAsync(DynamicClaimsContributeContext context);
}
