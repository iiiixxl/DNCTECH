using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Refit_Demo.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddRefitDemoJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<JwtTokenService>();

        var bearerSection = configuration.GetSection(
            $"Authentication:Schemes:{JwtBearerDefaults.AuthenticationScheme}");
        var keyBase64 = bearerSection["SigningKeys:0:Value"]!;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = bearerSection["ValidIssuer"],
                    ValidAudience = bearerSection["ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(keyBase64))
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddRefitDemoSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Refit Demo",
                Version = "v1",
                Description = "独立 Web 项目：JWT 登录 + Refit 出站调用 FakeRemote（本机假远端）"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header. Example: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
