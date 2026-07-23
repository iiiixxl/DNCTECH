using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Refit_Demo.Auth;

/// <summary>
/// JWT 登录 Demo。账号：admin / 123456。
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly JwtTokenService _tokenService;

    public AuthController(JwtTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username != "admin" || request.Password != "123456")
        {
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        var (token, expiresUtc) = _tokenService.GenerateToken(request.Username, "Admin", "User");

        return Ok(new
        {
            message = "登录成功。后续请求 Authorization: Bearer {token}",
            token,
            tokenType = "Bearer",
            expiresUtc
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            name = User.Identity?.Name,
            roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray(),
            claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
        });
    }
}
