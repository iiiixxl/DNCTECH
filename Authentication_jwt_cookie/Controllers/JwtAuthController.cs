using Authentication_jwt_cookie.Models;
using Authentication_jwt_cookie.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication_jwt_cookie.Controllers;

/// <summary>
/// JWT 认证：登录成功后返回 token，由客户端自行保存并在后续请求中携带。
/// </summary>
[ApiController]
[Route("[controller]")]
public class JwtAuthController : ControllerBase
{
    private readonly JwtTokenService _jwtTokenService;

    public JwtAuthController(JwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// JWT 登录。演示账号：admin / 123456
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username != "admin" || request.Password != "123456")
        {
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        var token = _jwtTokenService.GenerateToken(request.Username);

        return Ok(new
        {
            message = "登录成功，请在请求头携带 Authorization: Bearer {token}",
            token,
            tokenType = "Bearer",
            expiresInHours = 8
        });
    }
}
