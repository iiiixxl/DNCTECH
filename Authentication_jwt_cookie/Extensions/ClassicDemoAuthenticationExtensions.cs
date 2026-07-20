using System.Text.Json;
using Authentication_jwt_cookie.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Authentication_jwt_cookie.Extensions;

/// <summary>原 Cookie + JWT 双认证 Demo 注册。</summary>
public static class ClassicDemoAuthenticationExtensions
{
    /// <summary>
    /// Demo ①：Cookie（ITicketStore）+ 普通 JWT，无 session_id。
    /// </summary>
    public static IServiceCollection AddClassicDemoAuthentication(this IServiceCollection services)
    {
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

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((options, store) => options.SessionStore = store);

        services.AddSingleton<JwtTokenService>();

        return services;
    }
}
