using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Authentication_jwt_cookie.Services;

/// <summary>
/// JWT 令牌签发服务。
/// 签发参数与 appsettings.json 中 Authentication:Schemes:Bearer 保持一致，
/// 确保「签发」与「验签」使用同一套 Issuer / Audience / SigningKey。
/// </summary>
public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 为指定用户生成 JWT，客户端需在后续请求的 Authorization 头中携带。
    /// </summary>
    public string GenerateToken(string username)
    {
        var bearerSection = _configuration.GetSection(
            $"Authentication:Schemes:{JwtBearerDefaults.AuthenticationScheme}");

        var issuer = bearerSection["ValidIssuer"]!;
        var audience = bearerSection["ValidAudience"]!;
        var keyBase64 = bearerSection["SigningKeys:0:Value"]!;
        var expireHours = int.Parse(_configuration["Jwt:ExpireHours"] ?? "8");

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "User")
        };

        var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(keyBase64));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expireHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
