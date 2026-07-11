namespace Authorization_Extend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAppCookieAuthentication();

        // 原生 Policy 注册（供 NativeBookController 使用，独立可用）
        builder.Services.AddNativeBookPolicies();

        // ⚠️ 下面两套「动态权限」方案各自注册独立的 IAuthorizationPolicyProvider，
        //    该服务全局唯一（后注册者覆盖前者），因此二者互斥，演示时只启用其中一个。

        // 方案 A：仿 ABP 动态权限（Permissions 文件夹，供 BookController / ReportController 使用）
        // builder.Services.AddDynamicPermissions();

        // 方案 B：极简原生扩展动态权限（PolicyCodeAuthorization 文件夹，供 PolicyCodeUserController 使用）
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
