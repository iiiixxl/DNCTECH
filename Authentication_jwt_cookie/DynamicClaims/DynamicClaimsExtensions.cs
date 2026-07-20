using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>DynamicClaims / session_id Demo 的认证与管道注册（独立于 Classic Demo）。</summary>
public static class DynamicClaimsExtensions
{
    /// <summary>
    /// Demo ②：JWT + session_id 撤销 + 动态角色覆盖（见 <see cref="DynamicSessionController"/>）。
    /// </summary>
    public static IServiceCollection AddDynamicSessionDemoAuthentication(
        this IServiceCollection services,
        Action<DynamicClaimsOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<DynamicClaimsOptions>(_ => { });
        }

        services.AddSingleton<UserSessionStore>();
        services.AddSingleton<DemoUserClaimStore>();
        services.AddSingleton<SessionJwtTokenService>();
        services.AddSingleton<LoginClaimsPrincipalFactory>();

        // 登录时（非 Dynamic）：写入 session_id
        services.AddSingleton<IClaimsPrincipalContributor, SessionClaimsPrincipalContributor>();
        // 每个请求（Dynamic）：先校验 session，再覆盖最新角色（对齐 ABP 两 Contributor）
        services.AddSingleton<IDynamicClaimsPrincipalContributor, SessionDynamicClaimsContributor>();
        services.AddSingleton<IDynamicClaimsPrincipalContributor, IdentityDynamicClaimsContributor>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // 会话校验放在 DynamicClaims（对齐 ABP），不在 OnTokenValidated 里 Fail
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var message = string.IsNullOrEmpty(context.ErrorDescription)
                            ? "Invalid, expired or session revoked."
                            : context.ErrorDescription;
                        return context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            error = "Unauthorized",
                            message
                        }));
                    }
                };
            });

        return services;
    }

    public static IApplicationBuilder UseDynamicClaims(this IApplicationBuilder app) =>
        app.UseMiddleware<DynamicClaimsMiddleware>();
}

/// <summary>按 <see cref="AuthDemoMode"/> 只暴露当前 Demo 的控制器（Swagger/路由一致）。</summary>
public sealed class AuthDemoControllerConvention : IControllerModelConvention
{
    private readonly AuthDemoMode _mode;

    public AuthDemoControllerConvention(AuthDemoMode mode) => _mode = mode;

    public void Apply(ControllerModel controller)
    {
        var ns = controller.ControllerType.Namespace ?? string.Empty;
        var isDynamicClaims = ns.Contains("DynamicClaims", StringComparison.Ordinal);
        var keep = _mode == AuthDemoMode.DynamicSession ? isDynamicClaims : !isDynamicClaims;

        if (!keep)
        {
            controller.ApiExplorer.IsVisible = false;
            controller.Actions.Clear();
            controller.Selectors.Clear();
        }
    }
}
