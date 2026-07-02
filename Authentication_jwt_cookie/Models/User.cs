namespace Authentication_jwt_cookie.Models;

/// <summary>
/// 用户实体（演示数据，仅用于 Cookie 认证接口返回）。
/// </summary>
public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
