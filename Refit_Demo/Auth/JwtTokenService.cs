using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Refit_Demo.Auth;

/// <summary>
/// 本 Demo 自包含的 JWT 签发服务（不依赖其他项目）。
/// </summary>
public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresUtc) GenerateToken(string username, params string[] roles)
    {
        var bearerSection = _configuration.GetSection(
            $"Authentication:Schemes:{JwtBearerDefaults.AuthenticationScheme}");

        var issuer = bearerSection["ValidIssuer"]!;
        var audience = bearerSection["ValidAudience"]!;
        var keyBase64 = bearerSection["SigningKeys:0:Value"]!;
        var expireHours = int.Parse(_configuration["Jwt:ExpireHours"] ?? "8");
        var expiresUtc = DateTime.UtcNow.AddHours(expireHours);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Sub, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(keyBase64));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresUtc);
    }
}
