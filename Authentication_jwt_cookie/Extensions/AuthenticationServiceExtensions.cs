using System.Text.Json;
using Authentication_jwt_cookie.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Authentication_jwt_cookie.Extensions;

/// <summary>
/// 认证相关 DI 注册扩展，将 Program.cs 中的配置逻辑集中管理。
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// 注册 Cookie + JWT 双认证方案。
    /// </summary>
    /// <remarks>
    /// Cookie 模式说明（二选一，通过是否注册 ITicketStore 切换）：
    ///
    /// 【模式一 · 默认 Cookie】
    ///   - 不注册 ITicketStore
    ///   - Ticket 经 Data Protection 加密后直接写入 Cookie，服务端无会话
    ///
    /// 【模式二 · 服务端会话 · 当前启用】
    ///   - 注册 MemoryTicketStore（或 RedisTicketStore）
    ///   - Cookie 仅携带 SessionId，完整 Ticket 存在服务端
    ///
    /// JWT 验签参数从 appsettings.json 的 Authentication:Schemes:Bearer 自动加载；
    /// Events（如 401 响应格式）因无法写入配置文件，仍在代码中注册。
    /// </remarks>
    public static IServiceCollection AddAppAuthentication(this IServiceCollection services)
    {
        // --- 模式二：服务端会话存储 ---
        services.AddMemoryCache();
        services.AddSingleton<ITicketStore, MemoryTicketStore>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.RequireAuthenticatedSignIn = true;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "DNCTECH.Auth";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;

                // API 项目不使用登录页重定向，直接返回 HTTP 状态码
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // ValidIssuer / ValidAudience / SigningKeys 等由框架从 appsettings 自动绑定
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            error = "Unauthorized",
                            message = "Invalid or expired token."
                        }));
                    }
                };
            });

        // 模式二专用：将 Cookie 认证与 ITicketStore 绑定
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((options, store) => options.SessionStore = store);

        services.AddSingleton<JwtTokenService>();

        return services;
    }
}
