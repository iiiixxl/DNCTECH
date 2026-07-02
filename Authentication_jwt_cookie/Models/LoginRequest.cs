namespace Authentication_jwt_cookie.Models;

/// <summary>
/// 登录请求体，Cookie 与 JWT 登录接口共用。
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
