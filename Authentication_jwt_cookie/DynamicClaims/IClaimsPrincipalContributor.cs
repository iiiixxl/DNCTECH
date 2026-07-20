using System.Security.Claims;

namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>
/// 登录组 Claim 时的上下文。对应 ABP <c>AbpClaimsPrincipalContributorContext</c>（非 Dynamic）。
/// </summary>
public class ClaimsPrincipalContributorContext
{
    public ClaimsPrincipal ClaimsPrincipal { get; }

    public ClaimsPrincipalContributorContext(ClaimsPrincipal claimsPrincipal) =>
        ClaimsPrincipal = claimsPrincipal;
}

/// <summary>
/// 登录时向身份追加声明。对应 ABP <c>IAbpClaimsPrincipalContributor</c>。
/// 注意：与 <see cref="IDynamicClaimsPrincipalContributor"/>（每请求刷新）不是同一层。
/// </summary>
public interface IClaimsPrincipalContributor
{
    Task ContributeAsync(ClaimsPrincipalContributorContext context);
}
