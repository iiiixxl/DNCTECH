using System.Security.Claims;

namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>
/// 用权威源覆盖当前请求内存中的 Role 声明（Token 本身不变）。
/// 对应 ABP <c>IdentityDynamicClaimsPrincipalContributor</c>：
/// RemoveAll 旧角色 + Add 最新角色（空列表 = 角色全部撤销）。
/// </summary>
public class IdentityDynamicClaimsContributor : IDynamicClaimsPrincipalContributor
{
    private readonly DemoUserClaimStore _store;

    public IdentityDynamicClaimsContributor(DemoUserClaimStore store) => _store = store;

    public Task ContributeAsync(DynamicClaimsContributeContext context)
    {
        // 已被 Session Contributor 清空则不再处理
        if (context.Principal.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var identity = context.Principal.Identities.FirstOrDefault();
        if (identity is null)
        {
            return Task.CompletedTask;
        }

        var username = identity.Name
            ?? identity.FindFirst(ClaimTypes.Name)?.Value
            ?? identity.FindFirst("unique_name")?.Value;

        if (string.IsNullOrEmpty(username))
        {
            return Task.CompletedTask;
        }

        var roles = _store.GetRoles(username);

        RemoveAll(identity, ClaimTypes.Role);
        RemoveAll(identity, "role");

        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return Task.CompletedTask;
    }

    private static void RemoveAll(ClaimsIdentity identity, string claimType)
    {
        foreach (var claim in identity.FindAll(claimType).ToList())
        {
            identity.RemoveClaim(claim);
        }
    }
}
