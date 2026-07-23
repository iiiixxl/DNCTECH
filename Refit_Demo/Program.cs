using Refit_Demo.Auth;
using Refit_Demo.RefitDemo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRefitDemoJwtAuth(builder.Configuration);
builder.Services.AddRefitDemoSwagger();
builder.Services.AddRefitDemo(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Demo 用 HTTP 自调用（Refit BaseAddress=http://127.0.0.1:5235），避免 HTTPS 重定向丢掉 Authorization
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
