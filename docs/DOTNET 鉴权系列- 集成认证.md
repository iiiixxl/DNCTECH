# DOTNET 鉴权系列\- 集成认证

### 基本使用

框架为我们提供了很多默认的实现，通常在做一些中小项目，在不涉及OIDC不构建身份认证中心的场景下，通常选择使用Cookie和JWT的方式，当然内置了还有其他的一些实现，例如基于OAuth2\.0的实现，例如 谷歌、Facebook等用于支持第三方登录，还有基于OIDC的实现,不过在我们日常开发场景下，会接触使用一些，例如飞书、微信等登录，这个在框架中并没有实现，但是可以自己封装集成进框架中，如果要区分出他们有什么不同的话可能需要搞清楚一些概念。


1\. 什么是OIDC？

2\.什么是oAuth2\.0?



这里就不多做介绍，比较偏理论，梳理起来很头疼，但是个人觉得还是得静下心来找一些书籍或者项目自己研究下，不要太过于依赖AI，来的快去的也快，作为辅助还可以，后续有机会专门分享下自己的一些总结，
在我很长一段开发历程中，几乎没有接触过这些东西，相信大部分人和我一样，有时候项目小没必要关心这些，项目大也可能轮不着关心，甚至可能连实际搭一套登录鉴权体系的机会都没有，所以对这部分有点陌生，今天主要分享本地登录下的认证，就是自建系统登录加鉴权，不涉及第三方登录。


#### 使用cookie的认证方式

在介绍之前，我们必须先搞清楚一个概念，有状态和无状态，一句话就是指的是服务端要不要保存会话，下面是无状态的请求流程和特点。

![Image](https://internal-api-drive-stream.feishu.cn/space/api/box/stream/download/authcode/?code=ZGRmY2ZlNTNkNTkzZTkwZTc0ZjAxMzQzMTE0ZjU5MTlfMmQ1ZTM5OThlMTZmNDAxZTFkNmJiOWE1NWM4ZTc3NGNfSUQ6NzY1MzMzNTEwMTU1MTU0NTI5NF8xNzgxOTc0MDAyOjE3ODIwNjA0MDJfVjM)

特点：

- 服务端没有会话记录，重启应用不影响已发出的 Cookie（密钥还在就行）

- 退出登录 = 只删浏览器 Cookie，服务端没有会话

- 别人复制 Cookie，在你退出登录前可能还有效

- 多实例部署比较简单，每台机器有一样的密钥就行

- 因为Claims 都在里面，所以Cookie 很长



下面是有状态流程和特点

![Image](https://internal-api-drive-stream.feishu.cn/space/api/box/stream/download/authcode/?code=ZWRjZDViYzE0MGU1NDgxNzg4NTFmZjBmY2QzOGMyM2NfNWQ1MDQ2NTI3YWYzMWQ4MDE1YWY2NWVhZmU2YjYyNGFfSUQ6NzY1MzMzNTI5MzExMzgxNDIzN18xNzgxOTc0MDAyOjE3ODIwNjA0MDJfVjM)



特点：

- 服务端有会话记录，能强制下线、踢人、查在线

- 退出登录 = 删服务端会话 \+ 清 Cookie，旧 Cookie 立刻失效

- 多实例部署要 Redis 等共享存储，否则会话查不到

- Cookie 通常比无状态略短，但还是长密文，不是理解的那种guid那种。

- 应用重启如果在内存的话会话全丢，用户要重新登录

可以对比表格看下

NET Core 的 Cookie 认证方案在默认配置下是无状态的，服务端不会存储认证票据，服务端只需要：

- 从 HTTP 请求中读取 Cookie

- 使用 `TicketDataFormat` 解密

- 验证票据是否过期

当然也可以使用有状态，，主要用于处理票据很大 ，超过浏览器 Cookie 大小限制 或需要做即时撤销的场景，例如服务端踢人下线，又或者是需要在多个服务实例间共享会话状态,

我们先介绍在NETCORE中Cookie认证使用无状态模式怎么做

##### 无状态cookie

集成思路

![Image](https://internal-api-drive-stream.feishu.cn/space/api/box/stream/download/authcode/?code=NWZhZjRkZjgzODEwODU0OWU5ZGM2NTQ0MWVkMjA3NmZfZjE0N2ZmY2NkZmQ1MWU5YzAwYTNlODZiYjdkNzllZGNfSUQ6NzY1MzMxODcyMDgzNTYzNjE4MV8xNzgxOTc0MDAyOjE3ODIwNjA0MDJfVjM)

1\.需要先在`Program`注册身份认证的核心服务以及常规配置，使中间件可以调用对应服务。

```C#
var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    // 设置默认的身份验证方案（用于验证用户身份）
    // 当控制器未显式指定 AuthenticationSchemes 时，系统自动使用 Cookie 方案
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    
    // 设置默认的质询方案（用于触发登录流程）
    // 当未认证用户访问受保护资源时，系统自动重定向到 Cookie 登录流程
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    
    // 要求用户必须通过 SignInManager.SignInAsync() 完成登录
    // 防止仅通过 ClaimsPrincipal 构造的"伪登录"绕过安全检查
    options.RequireAuthenticatedSignIn = true;
});



```

2\.将Cookie认证方案和它的配置注入容器

```JavaScript
// 添加Cookie 身份验证方案
authenticationBuilder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    // 设置 Cookie 的名称浏览器中显示为 CodeSource.Auth）
    // 不同应用必须使用唯一名称避免跨站冲突
    options.Cookie.Name = "CodeSource.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);//过期时间
    options.SlidingExpiration = true;//滑动过期
    
    //针对没认证但是访问资源，将原本的重定向改为直接返回 401 
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    
    // 针对登录了，但是没权限访问资源，将原本的重定向改为直接返回 403，避前端因重定向丢失原始请求上下文
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
```



3\.注册中间件，通过 `app.UseXXX()` 方法将认证与授权中间件按顺序加入**请求处理管道**，这里需要注意顺序，先认证，再授权，所以中间件必须也要这样定义，至于为什么，可以自行去补充下基础知识。

```C#
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
}

app.UseHttpsRedirection();
app.UseSwaggerUI();

//添加认证中间件
app.UseAuthentication();
//添加授权中间件
*app.UseAuthorization();*

app.MapControllers();
app.Run();
```

4\.然后在控制器中使用就好了,注意标记`Authorize`特性，然后认证`Scheme`选择`cookie`的方式，括号中的如果只有一种认证方式可以不写，因为在前面配置时设置了默认方案。

```C#
[ApiController]
[Route("**[controller]**")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.**AuthenticationScheme**)]
public class UserController : ControllerBase
{
    private static readonly User[] Users =
    [
        new User { Id = 1, Name = "张三", Email = "zhangsan@example.com" },
        new User { Id = 2, Name = "李四", Email = "lisi@example.com" },
        new User { Id = 3, Name = "王五", Email = "wangwu@example.com" }
    ];

    [HttpGet(Name = "GetUsers")]
    public IEnumerable<User> Get()
    {
        return Users;
    }
}
```

##### 有状态cookie

相比上面无状态模式，有状态模式需要额外加一些代码用来存储读写和过期清理，如果是单服务实例，可以直接存入本地内存，但需注意服务重启的话会导致票据丢失，用户直接退出登录了，如果服务多实例负载均衡的情况下需要引入分布式存储来共享，把票据存入Redis或者Memcached中，如果你坚持本地内存可能会破坏负载均衡策略。



集成思路

![Image](https://internal-api-drive-stream.feishu.cn/space/api/box/stream/download/authcode/?code=MTczZmIwNTZiMmRiYmM2ZjgxOGU0MWEwNWJhNjI3NzBfM2U0OTVkNWJhNmEzYzYyN2Y3YzNmODhlMWI4NjNjNWZfSUQ6NzY1MzMzNDc1MTU0OTQxMDI2Ml8xNzgxOTc0MDAyOjE3ODIwNjA0MDJfVjM)

1\.将认证方案，例如 Cookie和它的配置注入容器，跟无状态一模一样的套路

```JavaScript
var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    // 设置默认的身份验证方案（用于验证用户身份）
    // 当控制器未显式指定 AuthenticationSchemes 时，系统自动使用 Cookie 方案
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    
    // 设置默认的质询方案（用于触发登录流程）
    // 当未认证用户访问受保护资源时，系统自动重定向到 Cookie 登录流程
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    
    // 要求用户必须通过 SignInManager.SignInAsync() 完成登录
    // 防止仅通过 ClaimsPrincipal 构造的"伪登录"绕过安全检查
    options.RequireAuthenticatedSignIn = true;
});

// 添加Cookie 身份验证方案
authenticationBuilder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    // 设置 Cookie 的名称浏览器中显示为 CodeSource.Auth）
    // 不同应用必须使用唯一名称避免跨站冲突
    options.Cookie.Name = "CodeSource.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);//过期时间
    options.SlidingExpiration = true;//滑动过期
    
    //针对没认证但是访问资源，将原本的重定向改为直接返回 401 
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    
    // 针对登录了，但是没权限访问资源，将原本的重定向改为直接返回 403，避前端因重定向丢失原始请求上下文
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
    
});

```

2\.如果需要使用Cookie的有状态模式，框架的`CookieAuthenticationOptions`选项提供了`SessionStore` 配置来进行实现，他是ITicketStore类型，这个接口是框架提供给用户扩展的，主要用于认证票据存在哪，我们需要实现它，



```C#
public class MemoryTicketStore(private readonly IMemoryCache _cache) : ITicketStore
{
    private const string KeyPrefix = "auth-session:";

    //存
    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = $"{KeyPrefix}{Guid.NewGuid():N}";
        RenewAsync(key, ticket);
        return Task.FromResult(key);
    }

    //更新
    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var expires = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddHours(8);
        _cache.Set(key, ticket, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expires
        });
        return Task.CompletedTask;
    }
    
    //查询
    public Task<AuthenticationTicket?> RetrieveAsync(string key)
        => Task.FromResult(_cache.Get<AuthenticationTicket?>(key));
    
    //删除
    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
```

3\.你同样可以自己实现Redis存储的逻辑，只需替换 `MemoryTicketStore` 为 `RedisTicketStore`），然后修改 `ITicketStore` 的注册，根本不需要改动身份验证配置，为了方便我使用的内存缓存，接下来需要将实现注入容器，再将它配置起来

```C#
// 注入组件
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITicketStore, MemoryTicketStore>();

//配置实例延迟执行，防止依赖的服务没注册
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.**AuthenticationScheme**)
    .Configure<ITicketStore>((options, store) => options.SessionStore = store);
```

4\.在控制器中使用和无状态模式一样使用

##### 使用`CookieAuthentication`登录实现

用`Cookie`认证的方式登录，不需要我们实现生成`cookie`，框架完全控制会话生命周期，`SignInAsync()` 触发标准化的 `Cookie` 生成流程，登录代码只需要构建`principal`就行。
① 构建 `ClaimsPrincipal`（含用户标识和权限声明）
② 调用 `SignInAsync()` 传递该主体

```C#
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    if (request.Username != "admin" || request.Password != "123456")
    {
        return Unauthorized(new { message = "用户名或密码错误" });
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.**Name**, request.Username),
        new Claim(ClaimTypes.**Role**, "User")
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.**AuthenticationScheme**);
    var principal = new ClaimsPrincipal(identity);

    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.**AuthenticationScheme**,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

    *// 模式一：return Ok(new { message = "登录成功" });*
*    *return Ok(new { message = "登录成功，会话已保存在服务端" }); *// 模式二（当前）*
}
```

#### 使用JWT的认证方式

##### 集成JWT

接下来我们继续看看JWT认证的方式如何在NETCORE中集成和实现，其实jwt也是无状态的，通常服务器一旦颁发，在token过期之前后期是不可控的。

集成思路

![Image](https://internal-api-drive-stream.feishu.cn/space/api/box/stream/download/authcode/?code=NDc2YjkyNDE5YzY3NTIzNmY3NDJmNmYzMzQ4NWM0YTFfNGY3ZWM1OTFlMzVhMjhiYjRjMTM0ZjhmMDBhNzQ5MThfSUQ6NzY1MzQyMTIyNzgwNTE1MDE3NV8xNzgxOTc0MDAyOjE3ODIwNjA0MDJfVjM)



1\.和集成Cookie认证一样，都需要先注册，然后启用认证中间件，而这里注册的是JWT相关的服务

```JavaScript
var authenticationBuilder =  builder.Services.AddAuthentication(options =>
{
    // 将默认方案设置为JWT
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});
authenticationBuilder.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,//验证签发者令牌中的 "iss" 字段必须与 ValidIssuer 完全匹配
        ValidateAudience = true,//是否验证受众保证令牌是发给当前服务的
        ValidateLifetime = true,//是否验证令牌有效期
        ValidateIssuerSigningKey = true,//是否验证令牌签名
        ValidIssuer = jwtIssuer,//合法签发者标识 "https://鉴权服务.com"
        ValidAudience = jwtAudience,//受众标识
        //令牌签名验证密钥对称加密
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("JWTjiamimiyao!"))
    };

    // JWT 认证流程中的事件钩子,未认证时触发
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            // 移除WW-Authenticate 避免暴露敏感信息
            context.HandleResponse(); 
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "未认证",
                message = "Token无效或者过期."
            }));
        }
    };
});
```

2\.直接接口中使用`Authorize(AuthenticationSchemes = "Bearer")]`就代表这一组接口都会启用jwt认证的方式来认证，框架就可以完成验签与claims的解析

```C#
[ApiController]
[Route("**[controller]**")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.**AuthenticationScheme**)]
public class ProductController : ControllerBase
{
    private static readonly Product[] Products =
    [
        new Product { Id = 1, Name = "机械键盘", Price = 599.00m },
    ];

    [HttpGet("{id:int}", Name = "GetProductById")]
    public ActionResult<Product> GetById(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        return product;
    }
}
```

那么集成进来之后，当有接口请求时，就会按照如下流程来执行

![Image](https://internal-api-drive-stream.feishu.cn/space/api/box/stream/download/authcode/?code=ZWQzZTdiNTQ5ZTllM2E4YmE0NDE5MDk0OWZjYWMxY2FfYWJhZTFmNjRmZjU3OTNjMDk4MDdlOWE0ZWNlNDZlNTVfSUQ6NzY1MzQxNTQ1Nzk0MDI0NTcwOV8xNzgxOTc0MDAyOjE3ODIwNjA0MDJfVjM)

因为定义了未授权事件，一旦触发就会收到友好的提示

![Image](https://internal-api-drive-stream.feishu.cn/space/api/box/stream/download/authcode/?code=MDM1NmNiOGJjZmRmNjIyODY3MjgyZTA4OTMwYTQwNGZfZjUxMjUzNDU3NDA4MDgyOWRiY2FlMmExMjNmYjM2MzRfSUQ6NzY1MzQxNzQ5OTI4Njk4MTgyNV8xNzgxOTc0MDAyOjE3ODIwNjA0MDJfVjM)

##### 使用`JwtAuthentication`登录实现

在使用jwt认证模式，登录实现机制和cookie有一个最大区别就是颁发凭证的逻辑需要自己完成，并且在登录时不需要手动构造 `ClaimsIdentity` 或 `ClaimsPrincipal` 对象。


1\.需要先实现生成token的服务，然后注入到DI容器

```C#

public class JwtTokenService(IConfiguration configuration)
{
    public string GenerateToken(string username) => 
        new JwtSecurityTokenHandler().WriteToken(
            new(
                issuer: configuration["Jwt:Issuer"]!,
                audience: configuration["Jwt:Audience"]!,
                claims: [
                    new(ClaimTypes.Name, username),
                    new(ClaimTypes.Role, "User")
                ],
                expires: DateTime.UtcNow.AddHours(
                    configuration.GetValue<int>("Jwt:ExpireHours", 8)
                ),
                signingCredentials: new(
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)
                    ),
                    SecurityAlgorithms.HmacSha256
                )
            )
        );
}
```

2\.在登录的代码中用户账密验证成功后签发token

```Java
public IActionResult Login([FromBody] LoginRequest request)
{
    if (request.Username != "admin" || request.Password != "123456")
    {
        return Unauthorized(new { message = "用户名或密码错误" });
    }

    var token = _jwtTokenService.GenerateToken(request.Username);

    return Ok(new
    {
        message = "登录成功，要在请求头携带 Authorization: Bearer {token}",
        token,
        tokenType = "Bearer",
        expiresInHours = 8
    });
}
```

#### 总结

在 \.NET Core 身份验证体系中，可以发现使用 `CookieAuthentication` 和`JwtAuthentication`，我们只需要配置就行了，有些概念我们完全不用担心，例如登录成功后怎么设置`Cookie`，然后请求到达如何验证Cookie或者Token是否合法，这些都是框架在背后帮我们完成了。

Cookie适用于有页面的例如MVC、 RazorPage，Token就比较适合纯Api，以及三方集成登录，有小伙伴可能会问，那我同时在系统加入Token和JWT能行吗？会不会有什么问题，其实不会有问题的，甚至可以更多,反而能灵活应对不同客户端的需求，如何做呢？

NETCORE的认证模块，允许我们使用多种`Scheme`，这些Shceme可以是框架提供的，也可以是我们自己扩展的，例如我在接口中同时引入`JWT`和`Cookie`,那么就代表这两种方式任一一种通过接口就能通过，注意不是并且的关系。

```C#
[Authorize(AuthenticationSchemes = JwtBearerDefaults.**AuthenticationScheme**)]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.**AuthenticationScheme**)]
public class ProductController : ControllerBase
{
}
```

也可以不同接口使用不同的认证策略，同时除了可以标记在控制器，也可以允许放在具体的`Action`上

```C#
[Authorize(AuthenticationSchemes = "CookieScheme")] // 仅允许 Cookie
public IActionResult MvcAction() { ... }

[Authorize(AuthenticationSchemes = "JwtScheme")]   // 仅允许 JWT
public IActionResult ApiAction() { ... }
```

甚至你可以配置所有的都必须经过认证，你可能会问，我有的接口不想认证怎么办，那就在接口上显式标记 `[AllowAnonymous]`

```C#
builder.Services.AddAuthorization(options =>
{
    // 设置默认策略必须要求已认证用户
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

再或者有少数情况使用过滤器也可以实现，例如你要实现0点到6点有些接口不允许访问就可以使用过滤器来实现

```C#
public class TimeRestrictionFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var now = DateTime.Now;
        if (now.Hour >= 0 && now.Hour < 6)
        {
            context.Result = new ForbidResult("禁止在 0:00-6:00 访问");
        }
    }
}
```

有时候标准的凭证传递方式不能实现，内置的 JWT 默认从 `Authorization: Bearer` 头读取 Token，Cookie 从请求头中读取。

如果你的程序客户端是 IoT 设备、老旧系统，必须通过请求体方式传递凭证咋办，那就用不了了，这个时候就可以自定义 `Scheme` 。

1\.先实现一个继承自`AuthenticationSchemeOptions`选项类

```C#
public class BodyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string ApiKeyFieldName { get; set; } = "apiKey";
    public string UserIdFieldName { get; set; } = "userId";
    public string ExpectedApiKey { get; set; } = "default-key";
}

```

2\.自定义认证处理Handler

```C#
public class BodyAuthenticationHandler : AuthenticationHandler<BodyAuthenticationOptions>
{
    public BodyAuthenticationHandler(
        IOptionsMonitor<BodyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 只处理 POST 请求
        if (!HttpContext.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        // 确保请求体可重复读取
        HttpContext.Request.EnableBuffering();

        // 读取并解析 JSON Body
        var body = await JsonDocument.ParseAsync(HttpContext.Request.Body);
        if (!body.RootElement.TryGetProperty(Options.ApiKeyFieldName, out var apiKeyProp))
        {
            return AuthenticateResult.Fail($"Missing field: {Options.ApiKeyFieldName}");
        }

        var providedKey = apiKeyProp.GetString();
        if (string.IsNullOrEmpty(providedKey) || providedKey != Options.ExpectedApiKey)
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }

        string userId = null;
        if (body.RootElement.TryGetProperty(Options.UserIdFieldName, out var userIdProp))
        {
            userId = userIdProp.GetString();
        }

        // 构造 Claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userId ?? "unknown")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
```

3\.然后在启动时注册到容器,应用的话就在接口上标记对应的scheme名称就可以

```JavaScript
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "BodyAuth";
})
.AddScheme<BodyAuthenticationOptions, BodyAuthenticationHandler>("BodyAuth", opt =>
{
    opt.ExpectedApiKey = "my-secret-key";
    opt.ApiKeyFieldName = "apiKey";
    opt.UserIdFieldName = "userId";
});
```



### 注册 

### 验证









|                     | 无状态（默认 AddCookie）         | 有状态（SessionStore / ITicketStore）             |
| ------------------- | -------------------------------- | ------------------------------------------------- |
| 服务端是否存会话    | 不存                             | 存（内存 / Redis / DB）                           |
| Cookie 里主要是什么 | 加密后的完整 Ticket（含 Claims） | 加密后的会话引用（SessionId）                     |
| 每次请求怎么认人    | 解密 Cookie → 直接得到 User      | 解密 Cookie → 拿 SessionId → 查服务端 → 得到 User |
