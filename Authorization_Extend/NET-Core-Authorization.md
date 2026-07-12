# .NET Core 授权（Authorization）

前两篇我们聊过认证鉴权模块，分别介绍了 Cookie、JWT 怎么接入及扩展，并且还分析了认证在请求链路里是怎么跑起来的。那么接下来进入 .NET Core 授权（Authorization）。简单来说就是这篇分享的所有东西，都建立在用户已经登录、Claims 已经在手的前提上，不要串台了。。

很好理解的区分：

**认证（Authentication)** 确认 **你是谁**，**授权（Authorization）** 决定 **你能做什么**，而在 HTTP 状态码上它们两也有对应关系：

| 场景 | 状态码 | 含义 |
|------|--------|------|
| 没登录 / 身份无效 | **401 Unauthorized** | 不知道你是谁，先认证 |
| 已登录，但不允许操作 | **403 Forbidden** | 知道你是谁，但没权限 |

很多小伙伴有过这种体验：没登录访问接口 → 401；登录了再访问 → 403。这说明身份已经确认，但 **授权没通过**。

---

## 授权在实际业务里的两个层面

"你能做什么"通常拆成两层，而且有先后顺序：认证通过 → 功能权限 → 资源权限

![image](https://img2024.cnblogs.com/blog/1264751/202607/1264751-20260711014212571-678915250.png)

### 1. 功能权限

功能权限回答的是，你能不能调用这个接口 / 能不能使用这个功能。在 Web 服务中一般对应 `端点（Endpoint）`，也就是具体的 API，比如：

- 有没有查看考勤列表这个菜单/接口的权限
- 有没有导出列表这个按钮对应的接口权限

.NET CORE 框架提供了基于角色 `Role` 和基于策略 `Policy` 的授权，是两种非常基础常见的访问控制方式，相信很多小伙伴应该接触或者使用过，都是在接口上标记特性，它们会在进入 Action 之前就拦截。没登录 → **401**，登录了，但角色/策略没有使用这个 **功能** 的权限 → **403**，

#### 1. 基于角色授权

基于角色授权规定当前用户必须拥有指定的角色如 Admin 或 User 才能访问资源，如果用逗号隔开 2 个，它们的关系是或，表示用户只要拥有其中任意一个角色即可通过授权。使用时只需要在接口中标记 `Authorize` 特性就能集成，下面表示只有管理员才能使用这个接口

```cs
[HttpGet]
[Authorize(Roles = "Admin")]
public IActionResult GetList()
{
    return Ok(new { approach = "simple-native-roles", data = Books });
}
```

#### 2. 基于策略授权

基于策略授权稍微灵活一点，但要先在 Program 里一个个注册。策略由一个或者多个 `IAuthorizationRequirement` 组成。在程序启动的时候，要将策略通过 `AddPolicy()` 方法注册到授权服务配置中

```cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("User.Delete", policy => policy.RequireRole("supAdmin"));
});
```

上面就是定义一个删除数据的策略，只有超级管理员才能使用，然后在接口中使用这个策略

```cs
[Authorize(Policy = "User.Delete")]
public IActionResult Delete(int id)
{
    return Ok(new { user= "sa", message = $"已删除{id}" });
}
```

配置可以理解为往类似下面的字典表中加入 key（策略名）和 value（权限规则），系统运行时框架就会拿着这个 key 去找对应的 value

![image](https://img2024.cnblogs.com/blog/1264751/202607/1264751-20260711205312184-799569214.png)

#### 3. 自定义动态权限逻辑

其实如果使用过或者仔细看上面代码的小伙伴应该会发现，基于角色、还是策略授权，代码在某种意义上是写 **死** 的，可以说完全是玩具，在简单的小项目里可以练练手，但只要项目稍微大点上点业务，问题立马就来了。如果不能发现问题所在，随便描述几个真实场景，一起脑补下

1. 假设你们已经有一个系统在线上运行，之前你们是基于策略授权的，导出报表的按钮，是只有管理员才能点的，今天产品找你说「导出报表」按钮，以后不是所有管理员都能点了，要单独控制。于是你新增一个权限点，回到 `Program` 加一行 `AddPolicy()` 改代码、走流程、重新发版。一个权限的调整，走了一遍完整的上线流程。

2. 如果你能接受上面的场景，也许真有人这么干，等着系统再跑几年，权限点从 10 个涨到 100 个，`Program` 里的 `AddPolicy()` 堆成了一坨山，谁都不敢动，一改就怕影响别的。

3. 更难顶的是，如果权限用 `RequireRole("Admin")` 写死了角色和权限的绑定关系。产品想给某个具体的人临时开个权限，这就傻眼了根本做不到，因为权限是绑在角色上的，不是绑在人身上的。你不会是想是不是再加一个 `RequireUser` 吧？

---

现在我们应该能理解，原生 `AddPolicy()` 最大的问题就是，权限在编译期写死的。想加一个权限点、调一个权限都得改代码重新编译。企业真实业务中，权限是需要运行时动态调整的东西，今天给张三开个口子，明天把李四的权限收回来，这些操作压根不应该让开发改代码的方式来实现。

那有没有办法，让加权限这件事不再需要改 Program、不再需要重新发版呢，就能消除 `AddPolicy()` 这样的模版代码？ <u>**策略名直接就是权限编码，权限归属存在库里，运行时查**</u> 给某个人加权限往库里插条数据就行，代码不用动，我们先把整个需求思路捋顺

**1. 每个接口在开发时对应一个权限编码。**

**2. 在程序启动时把编码加载到数据库。**

**3. 运行时管理员可以给不同用户、角色分配这个编码**。

**4. 请求接口时查询数据库，判断用户有没有分配这个权限编码（接口），有就能访问接口，没有就直接 403。**

实现这个需求一般有 2 种方式

**1. 自定义实现，最常见的方式就是，使用自定义过滤器或者中间件，在管道或者过滤器中获取访问接口的元数据，然后查库完成这个验权逻辑。**

**2. 扩展 .NETCORE 的授权系统。**

我们接下来要分享的就是使用第二种方式，基于 .NETCORE 的授权系统进行扩展，因为这种方式最优雅，也最符合 .NETCORE 平台风格，在这里我不会把这个需求的每一步都落地，因为每一步其实无论在单体还是微服务下，都是具有挑战性的，后续会使用真实的业务落地案例来分享，那么这篇内容主要描述的是如何扩展授权系统，接下来的内容，可能需要你有一定对 .NET Core 授权模块运行原理或者使用过的经验，才能明白我在干什么。如果你不了解，建议去了解下。最直接的方式，就是读一下授权模块的源码。我尽量说清楚，如果说不清楚可以留言一起讨论。

原生 `[Authorize(Policy = "User.Delete")]` 框架内部流程是这样的，请求进来，框架看到接口上面的 `[Authorize]` 上写了 `Policy = "User.Delete"` 就会拿着 `User.Delete` 这个名字，去找 `IAuthorizationPolicyProvider` 的接口，问它这个策略是什么样的，默认的 `PolicyProvider` 就会在 Program 中 `AddPolicy()` 注册过的字典里查，查到了就返回，查不到就报错。拿到策略后，执行策略里挂的 Requirement，交给对应的 Handler 再判断通不通过。

![image](https://img2024.cnblogs.com/blog/1264751/202607/1264751-20260711204529147-1075950034.png)

最大的问题就是 `PolicyProvider` 只认先注册好的策略，那我们能不能自己写一个 `PolicyProvider` 不查字典（上面黄色背景图），而是不管什么策略名，都现场给它造一个策略出来？ 造出来的策略都挂一个查数据库进行校验的 `Handler`，Handler 拿着策略名也就是权限编码，去数据库里查当前用户有没有这个权限。具体思路看下面的图

![image](https://img2024.cnblogs.com/blog/1264751/202607/1264751-20260711205939794-2082578484.png)

如果这样一改，`AddPolicy()` 就彻底不需要了，在接口上写 `[Authorize(Policy = "随便什么编码")]`，`PolicyProvider` 都能支持，`Handler` 都会去库里查使权限和代码完全解耦。整个方案就改动几个小文件，它们的关系如下面图，先有个整体印象后面逐个实现：

![image](https://img2024.cnblogs.com/blog/1264751/202607/1264751-20260711220620821-2071577666.png)

#### 4. 动态权限实现落地

**1. 自定义 `Requirement`**

实现 `IAuthorizationRequirement` 上面写着这个接口要求用户拥有哪个权限编码。它本身没有逻辑，只负责携带信息，理解为包装编码的对象。额。`IAuthorizationRequirement` 其实是个空接口，纯粹就是个标记。它的作用就是让框架能识别这是一个 `授权Requirement`，然后帮你找到到对应的 Handler。

```cs
public class PermissionCodeRequirement : IAuthorizationRequirement
{
    public PermissionCodeRequirement(string permissionCode)
    {
        PermissionCode = permissionCode;
    }

    public string PermissionCode { get; }
}
```

**2. 定义查库服务**

模拟 `user_permissions` 表，真实项目里就是数据库里的一张 `user_permissions` 表，记录哪个用户拥有哪些权限编码。wo 这里为了演示，用内存字典模拟一下，真实项目中可以换成 EF Core 查库、查 Redis 缓存都是一样的道理。

```cs
public interface IUserPermissionService
{
    // 模拟数据库 按用户 ID 查询是否拥有某权限编码。
    Task<bool> UserHasPermissionAsync(string userId, string permissionCode);

    Task<IReadOnlyList<string>> GetUserPermissionsAsync(string userId);
}
```

这一步是动态的关键步骤，想给张三加权限的话，往这张表插条数据就行，不用改代码重启，然后把权限编码抽成常量，避免到处写魔法值。

```cs
public static class PolicyCodePermissionNames
{
    public const string UserView = "User.View";
    public const string UserDelete = "User.Delete";
    public const string OrderCreate = "Order.Create";
}
```

**3. 写 `Handler`**

它是真正做判断的地方。它拿到 Requirement 后，从当前登录用户的 Claims 里把 userId 拿出来，再拿着 userId + 权限编码去查库，用户有没有这个权限，如果有就放行。

```cs
public class PermissionCodeHandler : AuthorizationHandler<PermissionCodeRequirement>
{
    private readonly IUserPermissionService _permissionService;

    public PermissionCodeHandler(IUserPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionCodeRequirement requirement)
    {
        // 从 Claims 里取 userId，认证登录时写进去的
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return; // 没取到 userId，直接不通过，说明都没登录
        }

        // 拿 userId + 权限编码去查数据库或者缓存，有就放行
        if (await _permissionService.UserHasPermissionAsync(userId, requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }
    }
}
```

**这里有 2 个细节**

1. `context.User.FindFirst(ClaimTypes.NameIdentifier)` 这个 `userId` 是认证阶段登录时写进 `Cookie` 的 Claim，如果你是 `jwt` 也是一样，你登录时会构造身份信息生成 `token`，认证时会解析 `token`，生成票据写到上下文，授权阶段直接从上下文取出来用，认证和授权就是这么串起来了，认证负责往里塞身份信息，授权取出来判断。

2. `context.Succeed(requirement)` 只会在通过时调用，如果不调，框架就会认为这个 requirement 不符合返回 403，不 Succeed，就是拒绝，突然想起有点像 linux 返回空白就是成功的这么个意思。。。。

**4. 写动态 `PolicyProvider`**

前面咱们其实还是实现原生 `Requirement + Handler` 的套路。真正动态起来的是这一步——我们要替换掉框架默认的 `IAuthorizationPolicyProvider`，让它不再依赖 `AddPolicy()` 注册的字典，而是按策略名 zi 直接构建 Policy。就可以说明策略压根不是提前注册的，而是每次请求现场造的。所以你想加多少权限编码都行

```cs
public class PolicyCodePolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PolicyCodePolicyProvider(IOptions<AuthorizationOptions> options)
    {
        // 保留默认 Provider 兜底
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName)) return await _fallback.GetPolicyAsync(policyName);

        // 1. 在 Program 里手动 AddPolicy() 注册的，优先用原生的
        var registered = await _fallback.GetPolicyAsync(policyName);
        if (registered is not null) return registered;

        // 2. 构造用户直接查数据库的 Requirement
        return new AuthorizationPolicyBuilder().AddRequirements(new PermissionCodeRequirement(policyName)).Build();
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
```

**5. 封装语法糖特性**

每次写 `[Authorize(Policy = "User.Delete")]` 有点啰嗦，而且 `Policy =` 这个写法看不出来它是个权限编码，我们自己封装一个特性，搞个语法糖特性继承自原有的 `Authorize`，然后把传进来的权限编码给 `Policy` 属性。本质上和 `[Authorize(Policy = "xxx")]` 一毛一样，只是写起来变成了 `[RequirePermissionCode("xxx")]`，一看就知道这是在配权限，不是在配策略。

```cs
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionCodeAttribute : AuthorizeAttribute
{
    public RequirePermissionCodeAttribute(string permissionCode)
    {
        Policy = permissionCode;
    }
}
```

**6. 注册服务到容器**

`Handler` 和 `Provider` 都需要注册进去。`IAuthorizationPolicyProvider` 要使用 `Replace` 而不是 `Add`。框架启动时默认注册了一个 `DefaultAuthorizationPolicyProvider`，我们把它换掉，而不是加一个，否则不清楚用哪个。

```cs
public static class PolicyCodeAuthorizationExtensions
{
    public static IServiceCollection AddPolicyCodeAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IUserPermissionService, InMemoryUserPermissionService>();
        services.AddScoped<IAuthorizationHandler, PermissionCodeHandler>();

        // 用我们自己的 Provider 替换掉框架默认的 IAuthorizationPolicyProvider
        services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, PolicyCodePolicyProvider>());
        return services;
    }
}
```

**7. 控制器中使用**

基本的框架就搭好了，然后在控制器里用起来，有小伙伴可能会问控制器上标了 `[Authorize(AuthenticationSchemes = "Cookie")]`，有什么区别，就是为了保证进来的是登录用户，方法上的 `[RequirePermissionCode]` 才是授权，判断这个登录用户有没有对应权限。

```cs
[ApiController]
[Route("api/policy-code")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class PolicyCodeUserController : ControllerBase
{
    [HttpGet]
    [RequirePermissionCode(PolicyCodePermissionNames.UserView)]
    public IActionResult GetUsers()
    {
        return Ok(new { data = new[] { new { Id = 1, Name = "张三" } } });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PolicyCodePermissionNames.UserDelete)]
    public IActionResult DeleteUser(int id)
    {
        return Ok(new { message = $"已删除用户 {id}" });
    }
}
```

**8. 完整请求流程**

我们串起来完整流程看一次请求，比如 user 只有 `User.View` 去删用户：

> **1. user 先登录，认证阶段往 Cookie 里写了 NameIdentifier = "yuxl" 这个 Claim。**
>
> **2. 带 Cookie 请求删除接口**
>
> **3. 认证中间件解开 `Cookie`，确认是登录用户，Claims 拿到手。**
>
> **4. 授权中间件看到方法上 `[Authorize(Policy = "User.Delete")]`，拿着 "User.Delete" 去问我们的 PolicyCodePolicyProvider。**
>
> **5. Provider 发现 Program 没注册过它，于是现场构造了 `PermissionCodeRequirement("User.Delete")` 的策略。**
>
> **6. 框架执行策略，调 `PermissionCodeHandler`。Handler 从 Claim 取出 "yuxl"，查数据库 "yuxl" 是否有 User.Delete，而库里 "yuxl" 只有 User.View，没有 User.Delete，Handler 就不会 Succeed。**
>
> **7. 框架判断不通过返回 403。**

**完整时序图**

![image](https://img2024.cnblogs.com/blog/1264751/202607/1264751-20260711230219309-2033876955.png)

#### 5. 总结

我们自己实现的这套非常简单的动态权限，其实就是使用原生授权的一个扩展点 `IAuthorizationPolicyProvider`。原生默认 `Provider` 只能获取 `AddPolicy()` 注册过的策略，我们把它换成一个自己的通用 `Provider`，就可以把权限从编译时期写死的代码变成了运行时查数据库了。不用改代码发版，插条数据就行。整个方案就几个步骤和文件，它适合权限直接绑定到用户、没有复杂权限树和权限管理 UI 的场景，也没有角色批量授权，也没有权限的动态注册。后续会结合 abp vnext 框架中的权限继续分享他是如何完整实现企业级授权系统的。

不过我们现在应该进一步知道了认证和授权是分开的，这篇分享的所有内容，必须都建立在上一篇认证已经把 userId 写进 Claim 的基础上，开头说过认证解决你是谁，授权解决你能干嘛，两个中间件先后配合起来才可以完成一套完整的鉴权体系。
