using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Authorization_Extend.TempPermissionAuthorization;

/// <summary>
/// 临时权限声明转换器：在认证之后、授权之前做两件事——
/// 1. 摘掉已过期的 TempPermission / TempValidUntil（时效性令牌自动失效）；
/// 2. 从授权库把「仍有效」的临时授权注入当前用户 Claims。
/// </summary>
/// <remarks>
/// 为什么不把临时权限永久写进 Cookie？
/// - Cookie/JWT 签发后内容固定，过期后仍会带着旧 Claim 直到重新登录；
/// - 转换器每次请求现算：库里一过期 / 一撤销，下次请求立刻生效，无需改认证流程。
/// </remarks>
public class TempPermissionClaimsTransformer : IClaimsTransformation
{
    private readonly ITempPermissionStore _store;

    public TempPermissionClaimsTransformer(ITempPermissionStore store)
    {
        _store = store;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        // 幂等：同一请求内 TransformAsync 可能被调多次
        if (identity.HasClaim(c => c.Type == TempClaimTypes.Enriched))
        {
            return principal;
        }

        // ① 先清掉 identity 上已经过期的临时 Claim（例如登录时写进 Cookie 的短时令牌）
        RemoveExpiredTempClaims(identity);

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            identity.AddClaim(new Claim(TempClaimTypes.Enriched, "true"));
            return principal;
        }

        // ② 从「授权库」注入仍有效的临时权限（主管授权后，下属下次请求自动带上）
        var activeGrants = await _store.GetActiveGrantsAsync(userId);
        foreach (var grant in activeGrants)
        {
            // 已有同权限声明则跳过，避免与 Cookie 里残留的有效 Claim 重复
            if (identity.HasClaim(TempClaimTypes.Permission, grant.Permission))
            {
                continue;
            }

            identity.AddClaim(new Claim(TempClaimTypes.Permission, grant.Permission));
            identity.AddClaim(new Claim(
                TempClaimTypes.ValidUntil,
                FormatValidUntil(grant.Permission, grant.ValidUntil)));
        }

        identity.AddClaim(new Claim(TempClaimTypes.Enriched, "true"));
        return principal;
    }

    /// <summary>
    /// 扫描并移除已过期的临时权限成对 Claim。
    /// </summary>
    private static void RemoveExpiredTempClaims(ClaimsIdentity identity)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var untilClaim in identity.FindAll(TempClaimTypes.ValidUntil).ToList())
        {
            if (!TryParseValidUntil(untilClaim.Value, out var permission, out var until))
            {
                // 格式非法也摘掉，避免脏数据一直挂着
                identity.RemoveClaim(untilClaim);
                continue;
            }

            if (until < now)
            {
                expiredPermissions.Add(permission);
                identity.RemoveClaim(untilClaim);
            }
        }

        if (expiredPermissions.Count == 0)
        {
            return;
        }

        // 对应的 Permission Claim 一并摘掉（用户示例里只删了 ValidUntil，这里补全）
        foreach (var claim in identity.FindAll(TempClaimTypes.Permission).ToList())
        {
            if (expiredPermissions.Contains(claim.Value))
            {
                identity.RemoveClaim(claim);
            }
        }
    }

    /// <summary>写入格式：permission|ISO-8601，便于 Handler 按权限精确匹配时效。</summary>
    internal static string FormatValidUntil(string permission, DateTimeOffset validUntil)
        => $"{permission}|{validUntil.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)}";

    internal static bool TryParseValidUntil(
        string raw,
        out string permission,
        out DateTimeOffset validUntil)
    {
        permission = string.Empty;
        validUntil = default;

        var sep = raw.IndexOf('|');
        if (sep <= 0 || sep >= raw.Length - 1)
        {
            return false;
        }

        permission = raw[..sep];
        return DateTimeOffset.TryParse(
            raw[(sep + 1)..],
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out validUntil);
    }
}
