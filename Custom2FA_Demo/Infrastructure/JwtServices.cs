using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Custom2FA_Demo.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Custom2FA_Demo.Infrastructure;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "Custom2FA_Demo";
    public string Audience { get; set; } = "Custom2FA_Demo";
    public string SigningKey { get; set; } = "Custom2FA_Demo_Dev_Signing_Key_32bytes!!";
    public int AccessTokenMinutes { get; set; } = 60;
    public int MfaTicketMinutes { get; set; } = 5;
}

/// <summary>
/// 对应 Identity 临时 Cookie(TwoFactorUserIdScheme)。
/// purpose=mfa 的短命 JWT，只证明「密码已过、等待第二步」。
/// </summary>
public sealed class MfaTicketService(JwtOptions options) : IMfaTicketService
{
    public const string PurposeClaim = "purpose";
    public const string MfaPurpose = "mfa";

    public string CreateTicket(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(PurposeClaim, MfaPurpose)
        };
        return CreateToken(claims, TimeSpan.FromMinutes(options.MfaTicketMinutes));
    }

    public Guid? ValidateTicket(string ticket)
    {
        try
        {
            var principal = Validate(ticket);
            if (principal.FindFirst(PurposeClaim)?.Value != MfaPurpose) return null;
            var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(id, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }

    private ClaimsPrincipal Validate(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, ValidationParameters(), out _);
    }

    private string CreateToken(IEnumerable<Claim> claims, TimeSpan lifetime)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = options.Issuer,
        ValidateAudience = true,
        ValidAudience = options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
}

public sealed class AccessTokenService(JwtOptions options) : IAccessTokenService
{
    public string CreateAccessToken(ClaimsPrincipal principal)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        // 过滤掉临时 purpose，避免污染正式 token
        var claims = principal.Claims.Where(c => c.Type != MfaTicketService.PurposeClaim).ToList();
        var jwt = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(options.AccessTokenMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
