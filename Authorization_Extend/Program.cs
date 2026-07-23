namespace Authorization_Extend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAppCookieAuthentication();



        // 声明转换（ClaimsTransformation 文件夹，供 ClaimsDemoController 使用）
        // 认证后、授权前，从业务库补充细粒度声明；同样不替换 PolicyProvider，可共存
         //builder.Services.AddClaimsTransformation();

        // 原生 Policy 注册（供 NativeBookController 使用，独立可用）
        // builder.Services.AddNativeBookPolicies();

        // ⚠️ 下面两套「动态权限」方案各自注册独立的 IAuthorizationPolicyProvider，
        //    该服务全局唯一（后注册者覆盖前者），因此二者互斥，演示时只启用其中一个。

        // 方案 A：仿 ABP 动态权限（Permissions 文件夹，供 BookController / ReportController 使用）
        // builder.Services.AddDynamicPermissions();

        // 方案 B：极简原生扩展动态权限（PolicyCodeAuthorization 文件夹，供 PolicyCodeUserController 使用）
        //builder.Services.AddPolicyCodeAuthorization();

        // 基于资源的动态授权（ResourceBasedAuthorization 文件夹，供 ResourceOrderController 使用）
        // 不替换 PolicyProvider，与上面两套互不冲突，可与任意一套同时启用
        builder.Services.AddResourceBasedAuthorization();

        // 字段级动态授权（FieldLevelAuthorization 文件夹，供 FieldLevelEmployeeController 使用）
        // 同一资源按 FieldPermission Claim 差异化返回字段；不替换 PolicyProvider，可共存
        //builder.Services.AddFieldLevelAuthorization();


        // 临时权限授予（TempPermissionAuthorization 文件夹，供 TempPermissionController 使用）
        // 声明转换注入时效 Claim + Handler 校验 TempValidUntil；不替换 PolicyProvider，可共存
        // 注意：本模块会 Replace IClaimsTransformation 为 Composite，串联上面的声明转换器
        //builder.Services.AddTempPermissionAuthorization();

        // 自定义授权结果处理器（AuthResultHandler 文件夹，供 AuthResultDemoController 使用）
        // 替换 IAuthorizationMiddlewareResultHandler，把拒绝响应改成结构化 JSON；
        // 不替换 PolicyProvider，与上面各套方案可共存
       builder.Services.AddCustomAuthResultHandler();

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
