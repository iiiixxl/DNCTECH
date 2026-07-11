using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Authorization_Extend.ClaimsTransformation;

/// <summary>
/// 声明转换器：在认证完成、进入授权之前，把业务库里的细粒度身份（部门 / 细分角色 / 权限点）
/// 补进当前用户的 ClaimsPrincipal。解决「原始 Token 声明太粗，撑不起复杂业务权限」的问题。
/// </summary>
/// <remarks>
/// 执行时机：每次 AuthenticateAsync 成功后，框架都会调用本类的 TransformAsync。
/// 补出来的 Claim 只活在「当前请求」的内存里，不会写回 Cookie/Token——
/// 这正是它的价值：库里权限一改，下次请求立刻生效，无需重新登录。
/// </remarks>
public class PermissionClaimsTransformer : IClaimsTransformation
{
    private readonly IUserProfileService _profileService;

    public PermissionClaimsTransformer(IUserProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // 只处理已认证的身份，匿名请求原样返回
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        // ★ 幂等保护（最容易踩的坑）：TransformAsync 一个请求里可能被调用多次，
        //   不加判断就会重复往 identity 里塞同样的 Claim。用哨兵 Claim 标记「已处理」。
        if (identity.HasClaim(c => c.Type == EnrichedClaimTypes.Enriched))
        {
            return principal;
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return principal;
        }

        // 从「数据库」加载用户画像（真实场景可合并 LDAP / OAuth2 / 企业微信等多源信息）
        var profile = await _profileService.GetProfileAsync(userId);
        if (profile is null)
        {
            return principal;
        }

        // 补部门：让 role=Admin 也能区分出「财务管理员 / 内容管理员」
        identity.AddClaim(new Claim(EnrichedClaimTypes.Department, profile.Department));

        // 补细粒度角色（原始 Token 里没有）
        foreach (var role in profile.Roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        // 补权限点声明
        foreach (var permission in profile.Permissions)
        {
            identity.AddClaim(new Claim(EnrichedClaimTypes.Permission, permission));
        }

        // 打上哨兵，避免后续重复转换
        identity.AddClaim(new Claim(EnrichedClaimTypes.Enriched, "true"));

        return principal;
    }
}
