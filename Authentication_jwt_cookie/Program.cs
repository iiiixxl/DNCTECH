using Authentication_jwt_cookie.Extensions;

namespace Authentication_jwt_cookie;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Cookie + JWT 双认证（配置细节见 Extensions/AuthenticationServiceExtensions.cs）
        builder.Services.AddAppAuthentication();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // 认证中间件必须在 MapControllers 之前
        app.UseAuthentication();

        app.MapControllers();

        app.Run();
    }
}
