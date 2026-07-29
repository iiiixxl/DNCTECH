# Refit 深度学习指南

> 基于 [reactiveui/refit](https://github.com/reactiveui/refit) 官方 README、文档站与 V14 breaking-changes 整理。  
> 目标：会用基础/进阶特性 → 理解架构与请求链路 → 能做业务扩展 → 清楚不足与演进坑。  
> 官方入口：[GitHub](https://github.com/reactiveui/refit) · [文档站](https://reactiveui.github.io/refit/) · [breaking-changes](https://github.com/reactiveui/refit/blob/main/docs/breaking-changes.md)

---

## 0. Refit 是什么

Refit 是 **.NET 版 Retrofit**：用 **接口 + Attribute** 声明 REST 契约，由 **源码生成器**（或反射回退）生成基于 `HttpClient` 的实现。

**一句话**：你只写「要什么」，Refit 负责「怎么发 HTTP」。

```csharp
public interface IGitHubApi
{
    [Get("/users/{user}")]
    Task<User> GetUser(string user);
}

var api = RestService.For<IGitHubApi>("https://api.github.com");
var user = await api.GetUser("octocat");
```

| NuGet | 用途 |
|-------|------|
| `Refit` | 核心 + Source Generator + Analyzers |
| `Refit.HttpClientFactory` | `AddRefitClient` / DI 集成 |
| `Refit.Newtonsoft.Json` | 可选 Newtonsoft 序列化 |
| `Refit.Reflection` | V14+ 可选；无法内联生成的方法走反射请求构建 |

---

## 1. 基本特性

### 1.1 HTTP Verb 与路由

内置：`Get` / `Post` / `Put` / `Delete` / `Patch` / `Head`。

```csharp
[Get("/users/{user}")]
Task<User> GetUser(string user);

[Get("/group/{id}/users")]
Task<List<User>> GroupList([AliasAs("id")] int groupId, string sort);
// → /group/4/users?sort=desc
```

要点：

- `{id}` 路径替换；参数名与占位符比较 **不区分大小写**
- **未**用于路径替换的参数 → 自动变成 Query（与 Retrofit 不同，不必全部显式标注）
- `[AliasAs("id")]`：参数名与 URL 占位不一致时使用
- 对象属性可绑定进路径：`/group/{request.groupId}/users/{request.userId}`
- `{**page}` catch-all：保留路径中的 `/`（类型须为 `string`）
- 默认：路由占位无匹配参数会抛错；可设 `RefitSettings.AllowUnmatchedRouteParameters = true`，留给 Handler 改写

### 1.2 Body

```csharp
[Post("/users")]
Task<User> CreateUser([Body] User user);
```

常见序列化方式：

- JSON（默认 `System.Text.Json`）
- Form URL encoded
- 原始 `string` / `Stream` / `HttpContent`
- JSON Lines：`[Body(BodySerializationMethod.JsonLines)]`（NDJSON）

### 1.3 Headers（基础）

```csharp
[Headers("User-Agent: Awesome Octocat App")]
public interface IGitHubApi
{
    [Get("/users/{user}")]
    Task<User> GetUser(string user, [Header("Authorization")] string authorization);

    [Get("/users/{user}")]
    Task<User> GetUserBearer(string user, [Authorize("Bearer")] string token);
}
```

- 静态：接口或方法上的 `[Headers]`
- 动态：参数 `[Header]` / `[Authorize]` / `[HeaderCollection]`

### 1.4 创建客户端

```csharp
// 方式 A：直接创建
var api = RestService.For<IGitHubApi>("https://api.github.com");

// 方式 B：自带 HttpClient
var client = new HttpClient { BaseAddress = new Uri("https://api.github.com") };
var api2 = RestService.For<IGitHubApi>(client);

// 方式 C：推荐 —— HttpClientFactory（需 Refit.HttpClientFactory）
services.AddRefitClient<IGitHubApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.github.com"));
```

### 1.5 异常与响应

| 返回类型 | 非成功状态码行为 |
|----------|------------------|
| `Task<T>` | 默认抛 `ApiException`（含 `ValidationApiException` 等） |
| `Task<IApiResponse>` / `Task<IApiResponse<T>>` / `Task<ApiResponse<T>>` | **不抛**；自行看 `IsSuccessful` / `Error` |

```csharp
try
{
    var user = await api.GetUser("octocat");
}
catch (ApiException ex)
{
    // ex.StatusCode, ex.Content, ex.Uri, ex.HttpMethod
}
```

可用 `RefitSettings.ExceptionFactory` 自定义何时抛、抛什么。

---

## 2. 进阶特性

### 2.1 Query 进阶

**对象展开为 Query：**

```csharp
public class MyQueryParams
{
    [AliasAs("order")]
    public string SortOrder { get; set; }
    public int Limit { get; set; }
}

[Get("/group/{id}/users")]
Task<List<User>> GroupList(int id, MyQueryParams @params);
```

**`[Query]` flatten（带前缀）：**

```csharp
[Get("/group/{id}/users")]
Task<List<User>> GroupList(int id, [Query(".", "search")] MyQueryParams @params);
// → ?search.order=desc&search.Limit=10
```

其他：

- 集合：`CollectionFormat`（Multi / Csv / Ssv / Tsv / Pipes）
- 自定义：`UrlParameterFormatter` / `UrlParameterKeyFormatter`
- V14：Query 对象按 **声明类型** flatten（非运行时类型）；属性名可与序列化器的 `[JsonPropertyName]` 对齐

### 2.2 Headers / 鉴权横切（推荐做法）

单接口偶尔传 `[Header]` 可以；**大量端点**应使用 `DelegatingHandler`，避免每个方法塞 Token。

```csharp
class AuthHeaderHandler : DelegatingHandler
{
    private readonly IAuthTokenStore _store;

    public AuthHeaderHandler(IAuthTokenStore store) => _store = store;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _store.GetToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}

services.AddTransient<AuthHeaderHandler>();
services.AddRefitClient<ISomeApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddHttpMessageHandler<AuthHeaderHandler>();
```

也可在接口上挂 `[Headers("Authorization: Bearer")]`，再配 `RefitSettings.AuthorizationHeaderValueGetter` 统一取 Token。

### 2.3 `[Property]`：把状态传入 Handler / Polly

```csharp
[Get("/users/{user}")]
Task<User> GetUser(string user, [Property("Priority")] string priority);
```

Handler 从 `HttpRequestMessage.Options` / `Properties` 读取；可与 Polly.Context 联动，实现「按请求」的重试策略元数据。

### 2.4 Multipart 上传

使用 `ByteArrayPart` / `StreamPart` / `FileInfoPart` 等 `MultipartItem` 类型作为参数。

注意（V14）：调用方传入的 **Body/Multipart Stream 不再由 Refit 代为 Dispose**（避免误关调用方拥有的流）；请求消息本身仍会按 HTTP 管道规则释放其 Content。

### 2.5 流式响应

```csharp
[Get("/events")]
IAsyncEnumerable<EventDto> StreamEvents(CancellationToken ct);
```

支持 JSON 数组或 JSON Lines（按 Content-Type 等判断），适合 SSE/日志流式消费场景（具体能力以当前版本 README 为准）。

### 2.6 接口继承、泛型、DIM

- **继承**：子接口合并父方法；Headers 继承有优先级（更内层覆盖更外层）
- **泛型接口/方法**：可生成；部分复杂 Query/Form 形状仍可能需反射路径
- **Default Interface Methods**：用 `internal` Refit 方法 + public DIM 包装业务逻辑（格式化、默认参数等）

### 2.7 Source Generation 与 AOT

```csharp
// 仅使用已生成的实现，无反射回退（AOT 友好）
var api = RestService.ForGenerated<IGitHubApi>(httpClient);

// DI（需较新 Refit.HttpClientFactory）
services.AddRefitGeneratedClient<IGitHubApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.github.com"));
```

项目开关（一般保持默认）：

```xml
<!-- 关闭「生成期直接拼请求」，仍生成接口实现但走旧反射构建 -->
<RefitGeneratedRequestBuilding>false</RefitGeneratedRequestBuilding>

<!-- 完全关闭源码生成 -->
<DisableRefitSourceGenerator>true</DisableRefitSourceGenerator>
```

分析器会报告：非法路由反斜杠、多个 `CancellationToken`、错误的 `[HeaderCollection]` 类型等；V14 的 **RF006** 标出仍需反射的方法。

### 2.8 URL 解析模式

默认行为与裸 `HttpClient`/RFC3986 不完全一致。可切换：

```csharp
var settings = new RefitSettings
{
    UrlResolution = UrlResolutionMode.Rfc3986
};
```

### 2.9 其他扩展点

| 扩展点 | 作用 |
|--------|------|
| `IHttpContentSerializer` | 自定义序列化（或换 Newtonsoft 包） |
| `ExceptionFactory` / `DeserializationExceptionFactory` | 自定义异常 |
| `IReturnTypeAdapter` | 自定义返回类型适配 |
| `HttpRequestMessageOptions` | 请求选项键约定 |

---

## 3. 实现原理与架构

### 3.1 设计思想

1. **Interface as Contract**：接口即 HTTP 契约，编译期/运行时生成实现  
2. **关注点分离**：绑定与序列化在 Refit；鉴权/重试/日志在 `DelegatingHandler`  
3. **Source-generator first**：默认编译期生成，减少反射；反射变为可选回退  
4. **拥抱 HttpClient 生态**：不重复发明管道，复用 `IHttpClientFactory`、Polly、Resilience

### 3.2 架构抽象图

```text
┌─────────────────────────────────────────────────────────────┐
│  业务代码：IGitHubApi.GetUser("octocat")                     │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  编译期：Roslyn Source Generator                             │
│  → 生成 XxxClient : IGitHubApi                               │
│  → （默认）内联构建 HttpRequestMessage                        │
│  → 无法内联时 → Refit.Reflection 请求构建器（V14 opt-in）     │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  RestService.For / AddRefitClient / ForGenerated             │
│  + RefitSettings（序列化 / 异常 / 鉴权 Getter / URL 策略）     │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  Request Runner                                              │
│  路径替换 · Query · Headers · Body · Options/[Property]      │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  DelegatingHandler 管道                                      │
│  Auth · 租户 · Trace · Polly/Resilience · 日志               │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  HttpClient.SendAsync                                        │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  IHttpContentSerializer 反序列化                             │
│  → T  /  ApiResponse<T>  /  ApiException                     │
└─────────────────────────────────────────────────────────────┘
```

### 3.3 一次请求的业务链路

| 步骤 | 发生什么 | 你可扩展的点 |
|------|----------|--------------|
| 1 | 调用接口方法，进入生成类 | DIM 包装、Facade |
| 2 | 构建 `HttpRequestMessage` | Formatter、Serializer |
| 3 | 写入 Options（MethodInfo、Property、接口类型） | Handler 读状态 |
| 4 | Handler 管道 | Auth / 重试 / 熔断 / 日志 |
| 5 | 网络往返 | 自定义 HttpClient、Timeout |
| 6 | 处理响应 | ExceptionFactory、`IApiResponse` |

### 3.4 双路径演进（理解 V14 的关键）

| 路径 | 特点 | 适用 |
|------|------|------|
| Generated request building | 编译期拼请求，少/无反射，AOT 友好 | 绝大多数常规方法形状 |
| Reflection builder（`Refit.Reflection`） | 运行时分析 MethodInfo | 生成器尚不能安全建模的形状 |

无生成实现且走 `ForGenerated` → 明确失败（`InvalidOperationException`），避免静默回退到不安全路径。

---

## 4. 可扩展的实用业务功能

在 **不改 Refit 内核** 的前提下，常见落地：

| 扩展 | 做法 | 价值 |
|------|------|------|
| 统一鉴权 + Token 刷新 | `AuthHeaderHandler` + Token Store | 接口零样板 Authorization |
| 多租户 / 关联 ID | `[Property]` + Handler 写 `X-Tenant-Id` / TraceId | 可观测、租户隔离 |
| 弹性（重试/熔断） | `Microsoft.Extensions.Http.Resilience` 或 Polly | 应对 429 / 5xx |
| ProblemDetails 映射 | 自定义 `ExceptionFactory` → 业务异常 | 与 ASP.NET 错误模型对齐 |
| 按环境切换 BaseAddress | Options + `IHttpClientBuilder` | Dev / Staging / Prod 一套接口 |
| 契约测试 | Mock `HttpMessageHandler` 或 `WebApplicationFactory` | 不依赖对方服务也能测契约 |
| API 版本占位 | `AllowUnmatchedRouteParameters` + Handler 替换 `{version}` | 集中版本策略 |
| ABP / 微服务出站 | 模块内 `AddRefitClient` + 操作日志 Handler | 与 DI / UoW 生命周期一致 |

**推荐团队模板骨架：**

```text
IXxxApi                          ← 契约（仅 Attribute + DTO）
XxxAuthHandler                   ← 鉴权/刷新
XxxTelemetryHandler              ← TraceId / 耗时
Refit 注册扩展方法                 ← BaseAddress + Handler 顺序 + Resilience
业务 AppService 只依赖 IXxxApi
```

---

## 5. 不足与待完善点

### 5.1 能力边界

- 本质是 **类型安全的 HttpClient 薄封装**，不是完整 OpenAPI「一键生成全套 SDK」工具
- 不做 gRPC、复杂 GraphQL、双向流等非「REST over HTTP」主战场
- 生成覆盖面持续扩大，但 **并非 100%** 方法形状都能内联；复杂场景仍可能要 `Refit.Reflection`
- 序列化强依赖 STJ/Newtonsoft 配置；**Native AOT** 仍需 `JsonSerializerContext` / `JsonTypeInfo`

### 5.2 工程与体验坑

| 坑 | 说明 |
|----|------|
| `Task<T>` vs `IApiResponse<T>` | 异常语义不同，团队必须统一约定 |
| 大版本 breaking | V11–V14：AOT、反射拆包、Query flatten 语义、ValueTask 等 |
| 堆栈变浅 | 内联生成后 `ApiException` 少一帧，靠 `Uri` / `HttpMethod` 定位 |
| URL 默认模式 | 与裸 HttpClient/RFC3986 不一致时需显式 `UrlResolution` |
| Handler 顺序 | `AddHttpMessageHandler` 顺序影响管道，易配错 |
| 与 HttpClientFactory 分工 | 部分 `RefitSettings`（与 Handler 生命周期相关）在 Factory 模式下被忽略 |

### 5.3 建议落地姿态

1. 新项目：**源码生成默认开**，尽量 **不引用** `Refit.Reflection`  
2. AOT / 裁剪：`ForGenerated` / `AddRefitGeneratedClient` + STJ 源生成元数据  
3. 横切逻辑 **全部进 Handler**，业务层只依赖接口  
4. 锁定主版本并阅读对应 `breaking-changes.md` 再升级  

---

## 6. 建议学习顺序

| 阶段 | 内容 | 产出 |
|------|------|------|
| Day 1–2 | 接口 CRUD、Body/Query、`RestService` / `AddRefitClient`、`ApiException` | 能调通一个真实 API |
| Day 3–4 | Auth Handler、`IApiResponse`、Multipart、集合 Query | 团队级鉴权模板 |
| Day 5–6 | 看生成代码、`ForGenerated`、流式、Polly/Resilience | 理解双路径与 AOT |
| Day 7+ | RF006、ExceptionFactory、ABP 模块注册 | 「鉴权 + 重试 + ProblemDetails」标准包 |

**动手建议：**

1. 对着官方 README 每节写一个最小接口  
2. 在 IDE 中查看 Source Generator 生成的 `*.g.cs`  
3. 故意触发 RF006 / 非法 Attribute，理解分析器边界  
4. 用 `HttpMessageHandler` Mock 写 2–3 个契约测试  

---

## 7. 速查：最小 DI 注册（生产常用）

```csharp
services.AddTransient<AuthHeaderHandler>();

services.AddRefitClient<IGitHubApi>(new RefitSettings
{
    // ContentSerializer / ExceptionFactory / AuthorizationHeaderValueGetter ...
})
.ConfigureHttpClient(c =>
{
    c.BaseAddress = new Uri("https://api.github.com");
    c.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<AuthHeaderHandler>();
// .AddStandardResilienceHandler(); // 若使用 Http.Resilience
```

---

## 8. 参考资料

- 仓库：https://github.com/reactiveui/refit  
- 文档：https://reactiveui.github.io/refit/  
- 破坏性变更：https://github.com/reactiveui/refit/blob/main/docs/breaking-changes.md  
- 灵感来源：Square Retrofit（Android/Java）

---

*文档整理自官方公开资料，便于对照实践；升级大版本时请以当前 README / breaking-changes 为准。*
