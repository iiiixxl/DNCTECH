using Authentication_jwt_cookie.DynamicClaims;
using Authentication_jwt_cookie.Extensions;

namespace Authentication_jwt_cookie;

public class Program
{
    public static void Main(string[] args)
    {
        // ★★★ 在这里切换 Demo（二选一）★★★
        var demo = AuthDemoMode.DynamicSession;
        // var demo = AuthDemoMode.Classic;

        var builder = WebApplication.CreateBuilder(args);

        switch (demo)
        {
            case AuthDemoMode.Classic:
                // Controllers/：Auth、JwtAuth、User、Product
                builder.Services.AddClassicDemoAuthentication();
                break;

            case AuthDemoMode.DynamicSession:
                // DynamicClaims/：login / me / logout（JWT + session_id）
                builder.Services.AddDynamicSessionDemoAuthentication();
                break;
        }

        builder.Services.AddAuthorization();
        builder.Services.AddControllers(options =>
            options.Conventions.Add(new AuthDemoControllerConvention(demo)));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();

        // 仅 DynamicSession Demo 需要动态会话中间件
        if (demo == AuthDemoMode.DynamicSession)
        {
            app.UseDynamicClaims();
        }

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
