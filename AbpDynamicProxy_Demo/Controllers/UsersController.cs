using AbpDynamicProxy_Demo.Application;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Mvc;

namespace AbpDynamicProxy_Demo.Controllers;

/// <summary>
/// Controller 故意不标权限。鉴权发生在注入的 IUserAppService（代理）方法调用时。
/// </summary>
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserAppService _userAppService;

    public UsersController(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    /// <summary>
    /// 需要 Demo.Users
    /// Header: X-Permissions: Demo.Users
    /// </summary>
    [HttpGet]
    public Task<IReadOnlyList<string>> GetListAsync()
    {
        return _userAppService.GetListAsync();
    }

    /// <summary>
    /// 需要 Demo.Users + Demo.Users.Create
    /// Header: X-Permissions: Demo.Users,Demo.Users.Create
    /// </summary>
    [HttpPost]
    public Task<string> CreateAsync([FromQuery] string userName = "bob")
    {
        return _userAppService.CreateAsync(userName);
    }

    /// <summary>
    /// 需要 Demo.Users + Demo.Users.Delete
    /// Header: X-Permissions: Demo.Users,Demo.Users.Delete
    /// </summary>
    [HttpDelete("{userName}")]
    public Task DeleteAsync(string userName)
    {
        return _userAppService.DeleteAsync(userName);
    }

    /// <summary>
    /// 观察 DI 解析出来的是否是 Castle 代理。
    /// </summary>
    [HttpGet("proxy-info")]
    public object GetProxyInfo()
    {
        var type = _userAppService.GetType();
        return new
        {
            ResolvedType = type.FullName,
            IsCastleProxy = type.Name.Contains("Proxy", StringComparison.Ordinal)
                            || typeof(IProxyTargetAccessor).IsAssignableFrom(type),
            Tip = "若 IsCastleProxy=true，说明 DI 返回的是代理，方法调用会进拦截器管道。"
        };
    }
}
