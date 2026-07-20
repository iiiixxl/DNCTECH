using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>
/// 按已组好的 Principal 签发 JWT（session_id 应由 Contributor 事先写入）。
/// </summary>
public class SessionJwtTokenService
{
    private readonly IConfiguration _configuration;

    public SessionJwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public (string Token, DateTime ExpiresUtc) GenerateToken(ClaimsPrincipal principal)
    {
        var bearerSection = _configuration.GetSection(
            $"Authentication:Schemes:{JwtBearerDefaults.AuthenticationScheme}");

        var issuer = bearerSection["ValidIssuer"]!;
        var audience = bearerSection["ValidAudience"]!;
        var keyBase64 = bearerSection["SigningKeys:0:Value"]!;
        var expireHours = int.Parse(_configuration["Jwt:ExpireHours"] ?? "8");
        var expiresUtc = DateTime.UtcNow.AddHours(expireHours);

        var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(keyBase64));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: principal.Claims,
            expires: expiresUtc,
            signingCredentials: credentials);

        return (new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token), expiresUtc);
    }
}
