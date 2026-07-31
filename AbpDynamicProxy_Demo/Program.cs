using AbpDynamicProxy_Demo;
using AbpDynamicProxy_Demo.Application;
using AbpDynamicProxy_Demo.Authorization;
using AbpDynamicProxy_Demo.DependencyInjection;
using AbpDynamicProxy_Demo.DynamicProxy;
using Autofac;
using Autofac.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 关键：用 Autofac 作为 DI，才能 EnableInterfaceInterceptors + InterceptedBy
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 拦截器与鉴权相关服务（必须能被 Castle/Autofac 解析）
builder.Services.AddTransient<ICurrentPermissionAccessor, HeaderPermissionAccessor>();
builder.Services.AddTransient<IMethodInvocationAuthorizationService, MethodInvocationAuthorizationService>();
builder.Services.AddTransient<AuthorizationInterceptor>();
builder.Services.AddTransient<LoggingInterceptor>();
builder.Services.AddTransient(typeof(AbpAsyncDeterminationInterceptor<>));

// 对应 ABP: services.OnRegistered(AuthorizationInterceptorRegistrar.RegisterIfNeeded)
var registrationActions = new ServiceRegistrationActionList();
registrationActions.Add(AuthorizationInterceptorRegistrar.RegisterIfNeeded);
registrationActions.Add(LoggingInterceptorRegistrar.RegisterIfNeeded);

builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    // 对应 ABP 注册 AppService：跑 OnRegistered → EnableInterfaceInterceptors → InterceptedBy
    container.RegisterAbpStyleService<IUserAppService, UserAppService>(registrationActions);
});

var app = builder.Build();

app.UseMiddleware<AuthorizationExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
