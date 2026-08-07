using Custom2FA_Demo.Abstractions;
using Custom2FA_Demo.Infrastructure;
using Custom2FA_Demo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Custom2FA_Demo;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var jwtOptions = new JwtOptions();
        builder.Configuration.GetSection("Jwt").Bind(jwtOptions);
        builder.Services.AddSingleton(jwtOptions);

        builder.Services.AddSingleton<SqliteUserStore>();
        builder.Services.AddSingleton<IUserStore>(sp => sp.GetRequiredService<SqliteUserStore>());
        builder.Services.AddSingleton<IUserTokenStore>(sp => sp.GetRequiredService<SqliteUserStore>());
        builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        builder.Services.AddSingleton<IUserClaimsPrincipalFactory, UserClaimsPrincipalFactory>();
        builder.Services.AddSingleton<ITotpService, TotpService>();
        builder.Services.AddSingleton<IMfaTicketService, MfaTicketService>();
        builder.Services.AddSingleton<IAccessTokenService, AccessTokenService>();
        builder.Services.AddSingleton<SignInService>();

        var mfaTicketService = new MfaTicketService(jwtOptions);
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = mfaTicketService.ValidationParameters();
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        var purpose = ctx.Principal?.FindFirst(MfaTicketService.PurposeClaim)?.Value;
                        if (purpose == MfaTicketService.MfaPurpose)
                            ctx.Fail("mfa_ticket 不能当作 access_token 使用");
                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();

        var app = builder.Build();

        var store = app.Services.GetRequiredService<SqliteUserStore>();
        await store.EnsureMigratedAsync();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync();
    }
}
