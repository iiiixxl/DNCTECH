# AdapterConcept：用最简单的接口理解「适配器」

这一文件夹**不依赖 Castle**，只用普通 C# 接口，演示和 ABP 拦截器适配**同一件事**：

> 第三方有一套难看的拦截 API；我们想用自己干净的拦截 API；中间用适配器翻译。

## 对照 ABP

| 本文件夹 | ABP / Castle |
|----------|----------------|
| `Foreign*`（第三方） | Castle.Core 的 `IInterceptor` / `IInvocation` |
| `App*`（我们） | `IAbpInterceptor` / `IAbpMethodInvocation` |
| `ForeignToAppInterceptorAdapter` | **适配器①** `CastleAsyncAbpInterceptorAdapter` |
| `ForeignToAppInvocationAdapter` | **适配器②** `CastleAbpMethodInvocationAdapter` |
| `PermissionCheckInterceptor` | `AuthorizationInterceptor` |
| `ForeignProxy` | Castle 生成的 Proxy（这里手写简化） |

## 适配解决什么问题？

**问题：**  
第三方（Castle）说：「你要拦截，必须实现 `IForeignInterceptor.Intercept(ForeignInvocation)`。」  
我们说：「业务只想写 `IAppInterceptor.InterceptAsync(IAppInvocation)`，不想依赖你们的类型。」

**若不适配：**  
每个授权/日志/事务拦截器都直接实现第三方接口 → 全项目绑死第三方，异步也难统一。

**适配后：**  
- 第三方只看到 `ForeignToAppInterceptorAdapter`（它实现了第三方接口）  
- 我们的 `PermissionCheckInterceptor` **零第三方依赖**  
- 换「第三方」时，主要改适配器，不改业务横切

## 两个适配器分工

```text
ForeignProxy.Invoke
  → 只认 IForeignInterceptor
  → 适配器① ForeignToAppInterceptorAdapter.Intercept(ForeignInvocation)
       → new 适配器② ForeignToAppInvocationAdapter（包成 IAppInvocation）
       → 我们的 PermissionCheckInterceptor.InterceptAsync(IAppInvocation)
            → Check 权限
            → ProceedAsync()
                 → 适配器② 内部调用 ForeignInvocation.Continue()
                 → 真实 OrderService.Create
```

| 适配器 | 翻译什么 | 一句话 |
|--------|----------|--------|
| ① 拦截器适配 | 第三方拦截器接口 → 调用我们的拦截器 | 让第三方能「调到」我们 |
| ② 调用上下文适配 | 第三方 Invocation → 我们的 Invocation | 让我们能用统一模型读 Method/Proceed |

## 怎么跑

启动 Web 后：

```http
GET /api/adapter-concept/run
GET /api/adapter-concept/run?permissions=Demo.Users,Demo.Users.Create
GET /api/adapter-concept/deny
```

看返回 JSON 里的 **`trace` 数组**：每一步谁在干什么，顺序和 ABP 双适配器一致。

## 建议阅读顺序

1. `Foreign/` — 先看「第三方长什么样」  
2. `App/` — 再看「我们想要什么样」  
3. `Adapters/` — 两个适配器如何翻译  
4. `Demo/PermissionCheckInterceptor.cs` — 业务横切完全不引用 Foreign  
5. `AdapterConceptController.cs` — 组装并输出 trace  
