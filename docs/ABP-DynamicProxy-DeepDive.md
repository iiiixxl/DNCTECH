# ABP 动态代理鉴权机制深度解析

> 基于本仓库演示项目 `AbpDynamicProxy_Demo`，对照 **ABP vNext**（LMS 中 `abp_framwork1020`，约 10.x）源码说明。  
> 文档结构：**先讲透 Castle.Core 扩展与双适配器**（这是理解后续一切的地基），再讲库、用法、整体设计与业务价值。

---

## 1. Castle.Core 扩展链路与双适配器（最核心，先读）

这一章回答三个问题：

1. ABP **怎么用** Castle，而不是「重写」Castle？  
2. Demo / ABP 里 **两个适配器** 各自解决什么阻抗？  
3. 一次 `CreateAsync` 调用，从 Proxy 到 `AuthorizationInterceptor` 怎么一步步走？

### 1.1 结论先行

ABP **没有改 Castle 源码**。做法是：

1. 用 Autofac + Castle **生成代理**（壳）  
2. 用 Autofac `InterceptedBy` 挂上 **Castle 能认识的拦截器类型**  
3. 该类型内部再通过 **两层适配器**，转到 ABP 自己的 `IAbpInterceptor`  
4. 你写的 `AuthorizationInterceptor` / `UnitOfWorkInterceptor` **只依赖 ABP 抽象，不依赖 Castle API**

```text
Castle 负责：造代理、触发拦截、继续 Proceed
ABP 负责：定义 IAbpInterceptor、写业务横切、通过适配器接上 Castle
```

### 1.2 为什么必须有适配器：两套 API 对不上

| 维度 | Castle.Core 原生世界 | ABP 自己想要的世界 |
|------|----------------------|-------------------|
| 拦截器接口 | `IInterceptor` / `AsyncInterceptorBase` | `IAbpInterceptor` |
| 一次调用上下文 | `IInvocation` | `IAbpMethodInvocation` |
| 继续往下执行 | `Proceed()` 或 async 的 `proceed(invocation, proceedInfo)` | `ProceedAsync()` |
| 业务横切作者 | 必须懂 Castle | 只懂 ABP 抽象即可 |
| 异步 | 老式同步拦截器拦 `async` 容易踩坑 | 统一 `InterceptAsync` |

如果让 `AuthorizationInterceptor` 直接实现 Castle 的 `IInterceptor`：

- 每个横切（授权、UoW、校验、审计）都绑死 Castle  
- 异步处理要在每个拦截器里重复处理  
- 以后换代理库，业务横切全要改  

所以 ABP 选择 **适配器模式**：中间加转换层。

### 1.3 整条扩展链路总图

```text
┌─ 注册时（Autofac）─────────────────────────────────────┐
│ EnableInterfaceInterceptors()                           │
│ InterceptedBy(                                          │
│   typeof(AbpAsyncDeterminationInterceptor              │
│            <AuthorizationInterceptor>))                 │
└────────────────────────────┬────────────────────────────┘
                             │ Resolve 时 Castle 生成代理
                             ▼
┌─ 运行时调用 Proxy.CreateAsync(...) ─────────────────────┐
│                                                         │
│ ① AbpAsyncDeterminationInterceptor<T>                   │
│    └─ 继承 AsyncDeterminationInterceptor（Castle 异步包）│
│    └─ Castle / Autofac 「认识」的门面拦截器              │
│              │                                          │
│              ▼ 内部持有                                  │
│ ② CastleAsyncAbpInterceptorAdapter<T>   ★适配器①       │
│    └─ 继承 AsyncInterceptorBase                         │
│    └─ 把 Castle 的 InterceptAsync 回调                  │
│       转成对 IAbpInterceptor.InterceptAsync 的调用      │
│              │                                          │
│              ▼ 构造并传入                                │
│ ③ CastleAbpMethodInvocationAdapter      ★适配器②       │
│    └─ 实现 IAbpMethodInvocation                         │
│    └─ 把 IInvocation / proceed 包成 ABP 调用上下文      │
│              │                                          │
│              ▼                                          │
│ ④ AuthorizationInterceptor : IAbpInterceptor            │
│    └─ Check(Method) → ProceedAsync()                    │
│              │                                          │
│              ▼ ProceedAsync 可能再进下一拦截器            │
│ ⑤ 真实 UserAppService.CreateAsync                       │
└─────────────────────────────────────────────────────────┘
```

Demo 与 ABP 源码对照：

| 层级 | Demo 路径 | ABP 源码路径 |
|------|-----------|--------------|
| 门面 | `DynamicProxy/AbpAsyncDeterminationInterceptor.cs` | `Volo.Abp.Castle.Core/.../AbpAsyncDeterminationInterceptor.cs` |
| 适配器① | `DynamicProxy/CastleAsyncAbpInterceptorAdapter.cs` | 同名 |
| 适配器② | `DynamicProxy/CastleAbpMethodInvocationAdapter.cs` | 同名（ABP 还有 Base + WithReturnValue） |
| 业务横切 | `Authorization/AuthorizationInterceptor.cs` | `Volo.Abp.Authorization/.../AuthorizationInterceptor.cs` |
| ABP 抽象 | `IAbpInterceptor` / `IAbpMethodInvocation` | `Volo.Abp.DynamicProxy` |

### 1.4 门面：`AbpAsyncDeterminationInterceptor<T>`

```csharp
// Demo 与 ABP 几乎一字不差
public class AbpAsyncDeterminationInterceptor<TInterceptor> : AsyncDeterminationInterceptor
    where TInterceptor : IAbpInterceptor
{
    public AbpAsyncDeterminationInterceptor(TInterceptor abpInterceptor)
        : base(new CastleAsyncAbpInterceptorAdapter<TInterceptor>(abpInterceptor))
    {
    }
}
```

**它做什么：**

1. Autofac `InterceptedBy(typeof(AbpAsyncDeterminationInterceptor<AuthorizationInterceptor>))`  
   挂的是 **这个类型**（Castle 拦截器体系能识别）。  
2. 基类 `AsyncDeterminationInterceptor` 来自包 `Castle.Core.AsyncInterceptor`：  
   负责判断当前调用是同步还是异步，再分发到正确的异步拦截路径。  
3. 构造时把真正的逻辑交给 **适配器①**。

**为什么需要它：**

- Autofac.Extras.DynamicProxy / Castle 历史路径更熟悉「拦截器类型」  
- ABP 的 `IAbpInterceptor` Castle **不认识**  
- 所以要有一个「Castle 门面」，内部再转 ABP  

可以把它想成：**电源转接头的外壳插头**——墙上插座只认这个形状。

### 1.5 适配器①：`CastleAsyncAbpInterceptorAdapter<T>`（拦截器形态适配）

**职责：把「Castle 异步拦截器回调」适配成「调用 ABP 的 IAbpInterceptor」。**

```csharp
public class CastleAsyncAbpInterceptorAdapter<TInterceptor> : AsyncInterceptorBase
    where TInterceptor : IAbpInterceptor
{
    private readonly TInterceptor _abpInterceptor;

    // 无返回值：Task
    protected override async Task InterceptAsync(
        IInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task> proceed)
    {
        await _abpInterceptor.InterceptAsync(
            new CastleAbpMethodInvocationAdapter(invocation, proceedInfo, proceed));
    }

    // 有返回值：Task<TResult>
    protected override async Task<TResult> InterceptAsync<TResult>(
        IInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
    {
        var adapter = new CastleAbpMethodInvocationAdapterWithReturnValue<TResult>(
            invocation, proceedInfo, proceed);

        await _abpInterceptor.InterceptAsync(adapter);
        return (TResult)adapter.ReturnValue!;
    }
}
```

**逐项含义：**

| 元素 | 含义 |
|------|------|
| 继承 `AsyncInterceptorBase` | 进入 Castle 异步拦截体系，正确 `await` |
| `_abpInterceptor` | 真正的 ABP 横切，如 `AuthorizationInterceptor` |
| `IInvocation` | Castle 给的「这次调用」原始对象 |
| `proceed` | Castle 给的「继续往下走」委托（下一拦截器或真方法） |
| `new CastleAbpMethodInvocationAdapter(...)` | 立刻再包一层，变成 ABP 认识的上下文（适配器②） |
| 两个 `InterceptAsync` 重载 | 分别对应 `Task` 与 `Task<T>`，返回值要从 adapter 取回 |

**解决的痛点：**

- 老式同步 `IInterceptor.Intercept` 里如果直接调 async 方法却不 await，会出现「鉴权还没完业务已经跑了」或异常被吞  
- 业务拦截器不必知道 Castle 的 `proceedInfo` / `AsyncInterceptorBase`

**一句话：** 适配器① = **拦截器接口的翻译官**（Castle 拦截器语言 → ABP 拦截器语言）。

### 1.6 适配器②：`CastleAbpMethodInvocationAdapter`（调用上下文适配）

**职责：把 Castle 的 `IInvocation` + `proceed` 适配成 ABP 的 `IAbpMethodInvocation`。**

```csharp
public class CastleAbpMethodInvocationAdapter : IAbpMethodInvocation
{
    private readonly IInvocation _invocation;
    private readonly IInvocationProceedInfo _proceedInfo;
    private readonly Func<IInvocation, IInvocationProceedInfo, Task> _proceed;

    public object TargetObject => _invocation.InvocationTarget;

    // 接口代理时：优先用实现类上的 MethodInfo（才能读到类上的 [Authorize]）
    public MethodInfo Method =>
        _invocation.MethodInvocationTarget ?? _invocation.Method;

    public object?[] Arguments => _invocation.Arguments;

    public object? ReturnValue
    {
        get => _invocation.ReturnValue;
        set => _invocation.ReturnValue = value;
    }

    public async Task ProceedAsync()
    {
        // 关键：把 ABP 的 ProceedAsync 映射回 Castle 的 proceed
        await _proceed(_invocation, _proceedInfo);
    }
}
```

带返回值的版本 `CastleAbpMethodInvocationAdapterWithReturnValue<TResult>` 在 `ProceedAsync` 里会：

```csharp
ReturnValue = await _proceed(_invocation, _proceedInfo);
```

这样适配器①才能 `return (TResult)adapter.ReturnValue`。

**成员对照表：**

| `IAbpMethodInvocation` | 来自 Castle | ABP 横切怎么用 |
|------------------------|-------------|----------------|
| `Method` | `MethodInvocationTarget ?? Method` | 读 `[Authorize]` / 权限名 |
| `TargetObject` | `InvocationTarget` | 需要时拿真实实例 |
| `Arguments` | `Arguments` | 校验、审计、日志 |
| `ReturnValue` | `ReturnValue` | 异步有返回值时回传 |
| `ProceedAsync()` | 包装后的 `proceed(...)` | 鉴权通过后进入下一环 |

**特别注意 `Method` 的选取：**

- 接口代理时，Castle 的 `Method` 往往是 **接口方法**  
- 接口方法上通常 **没有** 你写在实现类上的 `[PermissionAuthorize]` / `[Authorize]`  
- 所以要用 `MethodInvocationTarget`（实现类方法），类上的特性才能被 `DeclaringType` 读到  

这就是「精准鉴权」能工作的细节之一。

**一句话：** 适配器② = **一次方法调用上下文的翻译官**（Castle Invocation → ABP MethodInvocation）。

### 1.7 两个适配器的分工（务必分清）

| | 适配器① `CastleAsyncAbpInterceptorAdapter` | 适配器② `CastleAbpMethodInvocationAdapter` |
|--|---------------------------------------------|---------------------------------------------|
| 适配对象 | **拦截器** | **一次调用** |
| 从 | Castle `AsyncInterceptorBase` 回调 | Castle `IInvocation` + `proceed` |
| 到 | `IAbpInterceptor.InterceptAsync(...)` | `IAbpMethodInvocation` |
| 人生目标 | 让 ABP 拦截器被 Castle 调起来 | 让 ABP 拦截器能用统一模型读 Method / Proceed |
| 类比 | 把「英式插头」转成「中式电器接口」 | 把「英式说明书」翻译成「中文说明书」 |

没有①：Castle 调不到你的 `AuthorizationInterceptor`。  
没有②：就算调到了，你的拦截器也只能继续写 Castle API，抽象就失败了。

### 1.8 ABP 业务拦截器只看到什么

```csharp
public class AuthorizationInterceptor : AbpInterceptor
{
    public override async Task InterceptAsync(IAbpMethodInvocation invocation)
    {
        await _authorizationService.CheckAsync(invocation.Method);
        await invocation.ProceedAsync();
    }
}
```

注意：这里 **完全没有** `IInvocation`、`AsyncInterceptorBase`、`proceedInfo`。  
这就是扩展 Castle 的最终目的——**横切作者活在 ABP 抽象里**。

### 1.9 用一次 `POST /api/users`（CreateAsync）把链路跑穿

假设请求头：`X-Permissions: Demo.Users,Demo.Users.Create`

```text
1. UsersController.CreateAsync
2. 调用 _userAppService.CreateAsync("bob")
   （_userAppService 运行时类型 = Castle.Proxies.IUserAppServiceProxy）

3. Castle Proxy 进入拦截器管道
4. 命中 AbpAsyncDeterminationInterceptor<AuthorizationInterceptor>
5. 判定为异步调用 → 进入 CastleAsyncAbpInterceptorAdapter.InterceptAsync<string>(...)
6. new CastleAbpMethodInvocationAdapterWithReturnValue<string>(...)
7. AuthorizationInterceptor.InterceptAsync(abpInvocation)
   7.1 abpInvocation.Method = UserAppService.CreateAsync 的 MethodInfo
   7.2 收集特性：方法上 Demo.Users.Create + 类上 Demo.Users
   7.3 Header 里两个都有 → 通过
   7.4 await abpInvocation.ProceedAsync()
        → 内部调用 Castle proceed
        → 若还有 LoggingInterceptor，先过它
        → 最后进入真实 UserAppService.CreateAsync
8. 返回值沿适配器② → 适配器① → Proxy → Controller → HTTP 200
```

若 Header 只有 `Demo.Users`：

- 7.3 发现缺 `Demo.Users.Create` → `AuthorizationException`  
- `ProceedAsync` **不会执行** → 真实业务方法根本进不去 → Middleware 变成 403  

### 1.10 注册侧：Autofac 如何把这套挂上去

Demo（对应 ABP `AbpRegistrationBuilderExtensions.AddInterceptors`）：

```csharp
registration.EnableInterfaceInterceptors();

foreach (var interceptorType in context.Interceptors) // 如 AuthorizationInterceptor
{
    var castleInterceptorType =
        typeof(AbpAsyncDeterminationInterceptor<>).MakeGenericType(interceptorType);

    registration.InterceptedBy(castleInterceptorType);
}
```

含义：

1. `EnableInterfaceInterceptors`：告诉 Autofac，解析该接口时生成 Castle 接口代理  
2. `InterceptedBy(门面类型)`：代理上挂 `AbpAsyncDeterminationInterceptor<你的ABP拦截器>`  
3. 门面构造需要 `TInterceptor` 实例 → 从容器解析 `AuthorizationInterceptor`  
4. 门面内部 `new CastleAsyncAbpInterceptorAdapter<T>(abpInterceptor)`  

所以：**DI 解析出代理**，是因为注册时已经 `Enable*Interceptors` + `InterceptedBy`；不是 Controller 魔法。

### 1.11 和「只学授权特性」相比，理解适配器的收益

1. 调试时看到 `Castle.Proxies.xxx`、`AbpAsyncDeterminationInterceptor` 不再懵。  
2. 知道断点该打在：  
   - 适配器①（确认 Castle 是否进得来）  
   - `AuthorizationInterceptor`（确认鉴权逻辑）  
   - 真实 AppService（确认是否 Proceed 成功）  
3. 自己写新横切时：实现 `IAbpInterceptor` 即可，**不要**直接实现 Castle `IInterceptor`（除非你在造新的适配基础设施）。  
4. 能向别人讲清：ABP 是 **扩展 Castle（适配）**，不是 fork Castle。

### 1.12 建议对照阅读的源码文件

**Demo：**

1. `DynamicProxy/AbpAsyncDeterminationInterceptor.cs`  
2. `DynamicProxy/CastleAsyncAbpInterceptorAdapter.cs`  
3. `DynamicProxy/CastleAbpMethodInvocationAdapter.cs`  
4. `Authorization/AuthorizationInterceptor.cs`  
5. `DependencyInjection/AutofacAbpRegistrationExtensions.cs`  

**ABP（LMS 仓库）：**

1. `abp_framwork1020/framework/src/Volo.Abp.Castle.Core/.../AbpAsyncDeterminationInterceptor.cs`  
2. `.../CastleAsyncAbpInterceptorAdapter.cs`  
3. `.../CastleAbpMethodInvocationAdapter.cs` + `CastleAbpMethodInvocationAdapterBase.cs`  
4. `.../Volo.Abp.Authorization/.../AuthorizationInterceptor.cs`  
5. `.../Volo.Abp.Autofac/.../AbpRegistrationBuilderExtensions.cs`（`AddInterceptors`）

---

## 2. 使用了哪些库、技术

### 2.1 本 Demo 实际依赖

| 库 / 技术 | 版本（Demo） | 作用 |
|-----------|-------------|------|
| **ASP.NET Core Web API** | net8.0 | HTTP 入口、Controller、中间件 |
| **Autofac** | 经 `Autofac.Extensions.DependencyInjection` 9.0 | 替换默认 DI，支持「解析时生成代理」 |
| **Autofac.Extras.DynamicProxy** | 7.1 | `EnableInterfaceInterceptors` / `InterceptedBy` |
| **Castle.Core** | 随 DynamicProxy 引入 | 真正生成代理类型（`Castle.Proxies.xxxProxy`） |
| **Castle.Core.AsyncInterceptor** | 2.1 | 正确拦截 `async Task` / `Task<T>`；提供 `AsyncDeterminationInterceptor` |
| **Swagger / Swashbuckle** | 6.6 | 调接口演示 |

### 2.2 技术概念

| 概念 | 含义 |
|------|------|
| **AOP** | 横切插入鉴权 / 日志 / 事务 |
| **动态代理** | 运行时生成壳类型，调用先经壳再进真对象 |
| **拦截器管道** | 多个拦截器依次 `ProceedAsync` |
| **适配器模式** | 第 1 章双适配器：隔离 Castle 与 ABP 抽象 |
| **声明式权限** | `[Authorize]` / `[PermissionAuthorize]`，而不是手写 if |
| **DI 代理边界** | 只有容器解析出的是代理；`new` 出来的不是 |

### 2.3 ABP vNext 对应组件

| Demo | ABP |
|------|-----|
| `IAbpInterceptor` | `Volo.Abp.DynamicProxy.IAbpInterceptor` |
| 双适配器 + 门面 | `Volo.Abp.Castle.Core` |
| `AuthorizationInterceptor` | `Volo.Abp.Authorization.AuthorizationInterceptor` |
| Registrar + OnRegistered | `AuthorizationInterceptorRegistrar` + `AbpAuthorizationModule` |
| `AddInterceptors` | `AbpRegistrationBuilderExtensions` |

ABP 还额外有（本 Demo 未完整模拟）：完整 PermissionStore/Policy、UoW/Validation/Auditing 全套拦截器、模块约定注册、类代理等。

---

## 3. 怎么使用，以及对比 ABP vNext 实现到什么程度

### 3.1 启动与调试

```bash
cd AbpDynamicProxy_Demo
dotnet run
```

- Swagger：`http://localhost:5183/swagger`  
- 或用 `AbpDynamicProxy_Demo.http`

请求头模拟权限：

```http
X-Permissions: Demo.Users,Demo.Users.Create
```

| 接口 | 需要的权限 | 预期 |
|------|-----------|------|
| `GET /api/users` | `Demo.Users` | 无头 → 403；有 Default → 200 |
| `POST /api/users?userName=bob` | `Demo.Users` + `Demo.Users.Create` | 只有 Default → 403 |
| `DELETE /api/users/bob` | `Demo.Users` + `Demo.Users.Delete` | 缺 Delete → 403 |
| `GET /api/users/proxy-info` | 无 | `isCastleProxy: true`，类型名含 `IUserAppServiceProxy` |

### 3.2 业务侧怎么用（对齐 ABP）

1. 权限标在 AppService（类级 Default + 方法级操作）  
2. Controller 只转发，可不标权限  
3. 必须 DI 注入，禁止 `new` 应用服务  

### 3.3 对比 ABP vNext：Demo 实现到什么程度

| 能力点 | ABP vNext | 本 Demo |
|--------|-----------|---------|
| Castle 扩展 + 双适配器 | ✅ | ✅ 主干对齐 |
| `IAbpInterceptor` 抽象 | ✅ | ✅ |
| OnRegistered + Registrar | ✅ 全局自动 | ✅ 手动列表 |
| Autofac 出代理 | ✅ | ✅ |
| 接口代理 | ✅ | ✅ |
| 类代理 | ✅ | ❌ |
| 方法+类 Authorize AND | ✅ | ✅ |
| 完整 Permission/Policy | ✅ | ⚠️ Header 简化 |
| Auth+UoW+Validation 全管道 | ✅ | ⚠️ Auth+Logging |
| 模块约定注册 | ✅ | ❌ |

**结论：** Demo 足以吃透「Castle 扩展 + 代理鉴权」因果链；不是完整 ABP 框架替代品。

---

## 4. 设计流程、对象作用与细节（应用层视角）

> Castle/适配器细节见第 1 章；本章从「启动注册 → HTTP → 鉴权」看应用怎么用上这套。

### 4.1 总览

```text
启动：OnRegistered → Registrar 把 AuthorizationInterceptor 加入列表
     → EnableInterfaceInterceptors + InterceptedBy(门面)
解析：Castle.Proxies.IUserAppServiceProxy（内部是真 UserAppService）
调用：经第 1 章适配链路 → AuthorizationInterceptor → 真方法
```

### 4.2 分层对象（除适配器外）

#### 注册钩子（`DependencyInjection/`）

| 对象 | 作用 |
|------|------|
| `OnServiceRegistredContext` | 收集该服务要挂的拦截器类型列表 |
| `ServiceRegistrationActionList` | 对应 ABP `OnRegistered` |
| `RegisterAbpStyleService` | 跑钩子 + Autofac 开代理 |

#### 授权横切（`Authorization/`）

| 对象 | 作用 |
|------|------|
| `PermissionAuthorizeAttribute` | 声明权限名 |
| `AuthorizationInterceptorRegistrar` | 有特性才挂授权拦截器 |
| `AuthorizationInterceptor` | Check → Proceed |
| `MethodInvocationAuthorizationService` | 方法特性 ∪ 类特性，再校验 |
| `HeaderPermissionAccessor` | Demo 用请求头当「已授权权限」 |
| `LoggingInterceptor` | 演示第二层管道 |

**类级 + 方法级 = AND：**

```csharp
[PermissionAuthorize(UserPermissions.Default)]
public class UserAppService {
    [PermissionAuthorize(UserPermissions.Create)]
    public Task<string> CreateAsync(...) { }
}
```

创建需要同时有 `Demo.Users` 与 `Demo.Users.Create`。

#### 业务与入口

| 对象 | 作用 |
|------|------|
| `UserAppService` | 被代理的应用服务 |
| `UsersController` | 无权限特性；证明鉴权在代理层 |

### 4.3 「精准」指什么

- 拦截的是 **CLR 方法调用**，不是 URL  
- 用 `invocation.Method` 读特性  
- `CreateAsync` / `DeleteAsync` 权限互不影响  

所以自研 Controller 调 Pro AppService，取消权限仍 403。

### 4.4 代理鉴权 vs MVC 授权中间件

| | MVC 中间件 | AppService 代理拦截器 |
|--|-----------|----------------------|
| 触发点 | 进 Action 前 | DI 代理方法被调用时 |
| 依据 | Controller/Action | AppService 类/方法 |
| 范围 | 仅 HTTP | 一切经 DI 的调用 |

Identity Pro 习惯：细权限写在 AppService，HttpApi Controller 常不写。

---

## 5. 为什么这样设计：痛点、解决了什么、业务价值

### 5.1 痛点

1. 权限检查散落业务代码，易漏  
2. 只标 Controller：多入口 / 非 HTTP 调用会漏网  
3. 鉴权+事务+校验+审计手写 → 业务被淹没  
4. 横切直接依赖 Castle → 框架锁定、异步难写对  

### 5.2 怎么解决

| 痛点 | 手段 |
|------|------|
| 散落检查 | 声明式特性 + `AuthorizationInterceptor` |
| 多入口漏检 | 鉴权挂在 AppService 代理上 |
| 多种横切 | 拦截器管道可插拔 |
| 绑定 Castle | **双适配器**隔离（第 1 章） |
| 乱代理 | Registrar 条件挂载 |

### 5.3 为何 DI 必须是代理

拦截发生在调用边界：容器返回壳，壳里才能插入适配器与横切。  
`new` 真对象 = 绕过安全边界。

### 5.4 业务好处（LMS / Identity）

1. 自研 Controller 仍吃 Pro AppService 权限  
2. Default + Create/Delete 与权限树结构一致  
3. `TryDisablePermission`（定义裁剪）与拦截器（运行校验）正交  
4. 新横切优先加 `IAbpInterceptor`，而不是改遍业务方法  

### 5.5 代价

调试多一层代理类型；接口/虚方法约束；禁止随意 new；少量性能开销。

---

## 6. 建议学习路径

1. **先读本文第 1 章**（Castle + 双适配器），对照 Demo `DynamicProxy/`。  
2. 跑 `AbpDynamicProxy_Demo.http`，看 403/200。  
3. `GET /api/users/proxy-info` 确认代理类型。  
4. 断点顺序建议：  
   `CastleAsyncAbpInterceptorAdapter` → `AuthorizationInterceptor` → `UserAppService`  
5. 再读应用侧：`AutofacAbpRegistrationExtensions` → Registrar → Controller。  
6. 对照 ABP `Volo.Abp.Castle.Core` 与 `AuthorizationInterceptor`。

你应能用自己的话说明：

> ABP 用 Castle 造代理；用 `AbpAsyncDeterminationInterceptor` 挂到 Autofac；  
> 经 **适配器①** 调到 `IAbpInterceptor`，经 **适配器②** 把 `IInvocation` 变成 `IAbpMethodInvocation`；  
> 所以 AppService 上的 `[Authorize]` 能在方法调用时统一鉴权，即使 Controller 没标权限。

---

## 7. 附录：目录地图

```text
AbpDynamicProxy_Demo/
├── Program.cs
├── AuthorizationExceptionMiddleware.cs
├── DynamicProxy/          ← 第 1 章：门面 + 双适配器 + ABP 抽象
├── DependencyInjection/   ← 注册钩子 + Autofac 开代理
├── Authorization/         ← Registrar + AuthorizationInterceptor
├── Application/           ← 仿 Identity AppService
├── Controllers/           ← 无权限特性的 HTTP 入口
├── AbpDynamicProxy_Demo.http
├── README.md
└── ABP-DynamicProxy-DeepDive.md  ← 本文档
```

---

*文档版本：Castle 双适配器专章置顶；配合 `AbpDynamicProxy_Demo`；对照 ABP vNext 10.x。*
