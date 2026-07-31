using AbpDynamicProxy_Demo.Authorization;
using System.Net;

namespace AbpDynamicProxy_Demo;

/// <summary>
/// 把 AuthorizationException 转成 403，方便用 HTTP 调试。
/// </summary>
public class AuthorizationExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AuthorizationException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                requiredPermission = ex.PermissionName,
                message = ex.Message,
                hint = "请在请求头加上 X-Permissions，例如: Demo.Users,Demo.Users.Create"
            });
        }
    }
}
