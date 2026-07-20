using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>
/// 登录时组装 ClaimsPrincipal：基础声明 + <see cref="IClaimsPrincipalContributor"/> 链
/// （含 <see cref="SessionClaimsPrincipalContributor"/> 写入 session_id）。
/// 对应 ABP 登录流程里 ClaimsPrincipalFactory + Contributors。
/// </summary>
public class LoginClaimsPrincipalFactory
{
    private readonly IEnumerable<IClaimsPrincipalContributor> _contributors;

    public LoginClaimsPrincipalFactory(IEnumerable<IClaimsPrincipalContributor> contributors) =>
        _contributors = contributors;

    public async Task<ClaimsPrincipal> CreateAsync(string username)
    {
        // Token 快照角色：真实生效以 DemoUserClaimStore + IdentityDynamicClaimsContributor 为准
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "User")
            ],
            JwtBearerDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);
        var context = new ClaimsPrincipalContributorContext(principal);

        foreach (var contributor in _contributors)
        {
            await contributor.ContributeAsync(context);
        }

        return principal;
    }
}
