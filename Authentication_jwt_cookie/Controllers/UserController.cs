using Authentication_jwt_cookie.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication_jwt_cookie.Controllers;

/// <summary>
/// 仅 Cookie 认证可访问的用户列表接口。
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class UserController : ControllerBase
{
    private static readonly User[] Users =
    [
        new User { Id = 1, Name = "张三", Email = "zhangsan@example.com" },
        new User { Id = 2, Name = "李四", Email = "lisi@example.com" },
        new User { Id = 3, Name = "王五", Email = "wangwu@example.com" }
    ];

    [HttpGet(Name = "GetUsers")]
    public IEnumerable<User> Get()
    {
        return Users;
    }
}
