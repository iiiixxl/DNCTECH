namespace Authorization_Extend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAppCookieAuthentication();

        // 原生 Policy 注册（供 NativeBookController 使用）
        builder.Services.AddNativeBookPolicies();

        // 动态权限框架（ABP 风格 + 极简 PolicyCode 共用 PolicyProvider）
        builder.Services.AddDynamicPermissions();
        builder.Services.AddPolicyCodeAuthorization();

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
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
