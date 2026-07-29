# JWT 不可变怎么办？深入 ABP 的动态权限刷新与强制踢人机制

> 本文基于 ABP Framework 源码（abp_framwork1020）与 ABP Pro 源码（Volo.Abp.Identity.Pro）逐行分析，覆盖动态 Claims 刷新和 Session 强制失效两套机制的完整原理、设计取舍与实现细节。

---

## 目录

1. [背景：JWT 是把双刃剑](#一背景jwt-是把双刃剑)
2. [第一部分：动态 Claims —— 权限变更即时生效](#第一部分动态-claims--权限变更即时生效)
   - [痛点](#11-痛点)
   - [业界常见方案对比](#12-业界常见方案对比)
   - [ABP 的解法](#13-abp-的解法)
   - [源码逐层解析](#14-源码逐层解析)
   - [为什么这种方式更好](#15-为什么这种方式更好)
3. [第二部分：Session 校验 —— 强制踢人即时生效](#第二部分session-校验--强制踢人即时生效)
   - [痛点](#21-痛点)
   - [业界常见方案对比](#22-业界常见方案对比)
   - [ABP Pro 的解法](#23-abp-pro-的解法)
   - [源码逐层解析](#24-源码逐层解析)
   - [为什么这种方式更好](#25-为什么这种方式更好)
4. [两套机制的关系与协作](#三两套机制的关系与协作)
5. [总结](#四总结)

---

## 一、背景：JWT 是把双刃剑

JWT（JSON Web Token）是现代 Web 应用最流行的身份认证方案。它的优势显而易见：无状态、可横向扩展、不需要服务端存储 Session。

但它也有一个根本性的缺陷：**签发出去的 Token 是不可变的**。

Token 里写了什么，就是什么。服务端能做的只有「验签通过」或「验签失败」，没有办法在不让 Token 过期的前提下，修改 Token 里面的内容。

这带来了两类典型的工程问题：

| 问题 | 场景 | 传统 JWT 的困境 |
|------|------|----------------|
| **权限变更滞后** | 张三被撤销 Admin 角色，但他的 Token 还有 6 小时有效期 | 这 6 小时内张三仍然是 Admin |
| **无法强制踢人** | 管理员发现账号异常，想立刻让某个 Session 失效 | JWT 没有撤销机制，踢不掉 |

ABP 用两套机制分别解决了这两个问题，下面逐一拆解。

---

## 第一部分：动态 Claims —— 权限变更即时生效

### 1.1 痛点

假设这样一个场景：

```
09:00  张三登录，拿到 JWT，Token 里包含 role: Admin
10:00  李四（系统管理员）发现张三操作异常，立刻撤销了他的 Admin 角色
10:01  张三带着旧 Token 访问 /api/admin/users
```

**没有动态 Claims 的结果：**

`UseAuthentication` 解析 JWT，发现签名有效、未过期，于是把 Token 里的 `role: Admin` 老老实实放进 `HttpContext.User`。`UseAuthorization` 检查 `[Authorize(Roles="Admin")]`，通过，接口放行。

李四的操作形同虚设，张三最多到 Token 过期前（可能还有几个小时），都能以 Admin 身份操作系统。

这是 JWT 无状态设计带来的必然代价——**Token 本身存储了权限快照，一旦签发，服务端无法撤回**。

---

### 1.2 业界常见方案对比

在谈 ABP 的解法之前，先看看业界通常怎么处理这个问题，以及各自的代价：

#### 方案 A：缩短 Token 有效期（短 Token）

将 Access Token 有效期压缩到 5~15 分钟，依靠 Refresh Token 续期。

```
优点：权限变更最多 15 分钟内生效
缺点：
  - 每 15 分钟就要刷新一次，增加认证服务器压力
  - Refresh Token 本身也是凭据，需要安全存储和轮换
  - 并不能做到"立刻"生效，只能"快一点"生效
```

#### 方案 B：Token 黑名单（Revocation List）

在 Redis 或数据库里维护一个"已失效 Token 列表"，每次请求都查一下。

```
优点：可以做到立刻撤销
缺点：
  - 破坏了 JWT 的无状态优势，变成了有状态验证
  - Token 数量庞大时，黑名单维护成本高
  - 需要处理黑名单条目的过期清理
  - 分布式场景下，黑名单同步复杂
```

#### 方案 C：每次请求查数据库

每次请求都根据 Token 里的 `userId` 去数据库重新查最新角色。

```
优点：权限立刻生效
缺点：
  - 每请求一次就打一次数据库，高并发下数据库压力极大
  - 只加不删，旧 Token 里的声明还在，新查到的声明加进去，两者共存
  - 多个微服务都自己查，逻辑重复，数据不一致
  - 与 ASP.NET Core 认证模型（AuthenticationTicket）完全脱节
```

这三种方案要么权限变更不够及时，要么性能代价过大，要么工程实现复杂。ABP 选择了第四条路。

---

### 1.3 ABP 的解法

**核心思路：不动 Token，只改内存里的 ClaimsPrincipal**

ABP 的动态 Claims 机制本质上是一个「中间件拦截 + 缓存覆写」方案：

1. JWT 解析后，在内存里建一个 `ClaimsPrincipal` 对象（`HttpContext.User`），里面有旧的 `role: Admin`
2. 在 `UseAuthorization` 之前，专门跑一个中间件，从 **Redis 缓存**（而不是每次都查 DB）拿到该用户最新的权限
3. 把内存里的 `ClaimsPrincipal` **原地改写**，删掉旧的 `role: Admin`，写入新的 `role: User`
4. `UseAuthorization` 看到的 `HttpContext.User` 已经是改写后的内容，于是返回 403

JWT 字节本身从未被修改。如果你解码客户端持有的 JWT，里面还是 `role: Admin`。但服务端内存里，这次请求的身份已经变成了 `role: User`。**下一次请求也是同样的流程**，每次请求都独立刷新一次。

---

### 1.4 源码逐层解析

整个机制分为四层，从外到内依次是：

#### 第一层：`AbpDynamicClaimsMiddleware`（入口）

这是整个机制的入口，注册为 `app.UseDynamicClaims()`，必须放在 `UseAuthentication()` 之后、`UseAuthorization()` 之前。

```csharp
// AbpDynamicClaimsMiddleware.cs
public async override Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    if (context.User.Identity?.IsAuthenticated == true)  // 只处理已认证请求
    {
        if (IsDynamicClaimsEnabled)  // 必须显式开启
        {
            var authenticateResultFeature = context.Features.Get<IAuthenticateResultFeature>();
            var authenticationType = authenticateResultFeature?.AuthenticateResult?.Ticket?.AuthenticationScheme
                ?? context.User.Identity.AuthenticationType;

            if (authenticateResultFeature != null && !authenticationType.IsNullOrWhiteSpace())
            {
                var abpClaimsPrincipalFactory = context.RequestServices.GetRequiredService<IAbpClaimsPrincipalFactory>();
                
                // 关键：把当前的 context.User 传进去，让 Factory 在它上面原地修改
                var user = await abpClaimsPrincipalFactory.CreateDynamicAsync(context.User);

                // 同步更新 AuthenticationTicket，确保后续中间件也能读到新数据
                authenticateResultFeature.AuthenticateResult = AuthenticateResult.Success(
                    new AuthenticationTicket(user, ..., authenticationType));
            }

            // 如果改写后身份变成未认证（如 session 失效），则强制登出
            if (context.User.Identity?.IsAuthenticated == false)
            {
                await context.SignOutAsync(authenticationScheme.Name);
            }
        }
    }

    await next(context);  // 继续管道，此时 HttpContext.User 已是最新权限
}
```

**关键点**：`CreateDynamicAsync(context.User)` 传入的是 `context.User` 这个引用，Factory 在这个对象上**原地操作**，不创建新对象，所以 `HttpContext.User` 本身就随之改变了。

#### 第二层：`AbpClaimsPrincipalFactory`（分发器）

这是一个调度层，它决定运行哪些 Contributor：

```csharp
// AbpClaimsPrincipalFactory.cs
public virtual async Task<ClaimsPrincipal> InternalCreateAsync(
    AbpClaimsPrincipalFactoryOptions options,
    ClaimsPrincipal? existsClaimsPrincipal = null,
    bool isDynamic = false)
{
    // 复用传入的 existsClaimsPrincipal，不新建对象
    var claimsPrincipal = existsClaimsPrincipal ?? new ClaimsPrincipal(...);
    var context = new AbpClaimsPrincipalContributorContext(claimsPrincipal, ServiceProvider);

    if (!isDynamic)
    {
        // 登录时运行：静态 Contributor（写入初始 claims，如 sessionId）
        foreach (var contributorType in options.Contributors) { ... }
    }
    else
    {
        // 每次请求运行：动态 Contributor（从缓存/DB 刷新权限）
        foreach (var contributorType in options.DynamicContributors) { ... }
    }

    return context.ClaimsPrincipal;  // 返回同一个引用（已在原地改写）
}
```

动态和静态的 Contributor 是完全分开的两条链路：
- **静态**（`Contributors`）：登录签发 Token 时运行一次，负责构建初始 claims
- **动态**（`DynamicContributors`）：每次 HTTP 请求都运行，负责刷新 claims

#### 第三层：`AbpDynamicClaimsPrincipalContributorBase`（改写执行器）

基类里做了真正的「删旧加新」操作：

```csharp
// AbpDynamicClaimsPrincipalContributorBase.cs
protected virtual async Task AddDynamicClaimsAsync(
    AbpClaimsPrincipalContributorContext context,
    ClaimsIdentity identity,
    List<AbpDynamicClaim> dynamicClaims)
{
    // 第一步：处理 ClaimsMap，对齐声明类型名（如 "role" → AbpClaimTypes.Role）
    var options = context.GetRequiredService<IOptions<AbpClaimsPrincipalFactoryOptions>>().Value;
    foreach (var map in options.ClaimsMap)
    {
        await MapClaimAsync(identity, dynamicClaims, map.Key, map.Value.ToArray());
    }

    // 第二步：原地覆写
    foreach (var claimGroup in dynamicClaims.GroupBy(x => x.Type))
    {
        identity.RemoveAll(claimGroup.First().Type);  // 删掉 JWT 里的旧值
        identity.AddClaims(
            claimGroup
                .Where(c => c.Value != null)          // Value == null 表示「已撤销，不加」
                .Select(c => new Claim(claimGroup.First().Type, c.Value!))
        );
    }
}
```

`Value == null` 的设计很巧妙：缓存里存一个 `null` 值，表示「该角色已被撤销」，执行时就不会 `AddClaims`，达到删除效果，而不需要额外的标记位。

#### 第四层：`IdentityDynamicClaimsPrincipalContributor` + 缓存（数据来源）

具体的数据从哪里来，由各 Contributor 决定。Identity 模块内置的 Contributor 这样工作：

```csharp
// IdentityDynamicClaimsPrincipalContributor.cs
public async override Task ContributeAsync(AbpClaimsPrincipalContributorContext context)
{
    var identity = context.ClaimsPrincipal.Identities.FirstOrDefault();
    var userId = identity?.FindUserId();
    
    // 从缓存服务拿最新 claims（缓存 miss 时会查 DB 并写缓存）
    var dynamicClaims = await dynamicClaimsCache.GetAsync(userId.Value, identity.FindTenantId());
    
    await AddDynamicClaimsAsync(context, identity, dynamicClaims.Claims);
}
```

```csharp
// IdentityDynamicClaimsPrincipalContributorCache.cs（缓存层）
public virtual async Task<AbpDynamicClaimCacheItem> GetAsync(Guid userId, Guid? tenantId = null)
{
    return await DynamicClaimCache.GetOrAddAsync(
        AbpDynamicClaimCacheItem.CalculateCacheKey(userId, tenantId),  // key = "{tenantId}-{userId}"
        async () =>
        {
            // 缓存 miss：查数据库
            var user = await UserManager.GetByIdAsync(userId);
            var principal = await UserClaimsPrincipalFactory.CreateAsync(user);

            var dynamicClaims = new AbpDynamicClaimCacheItem();
            foreach (var claimType in AbpClaimsPrincipalFactoryOptions.Value.DynamicClaims)
            {
                var claims = principal.Claims.Where(x => x.Type == claimType).ToList();
                if (claims.Any())
                    dynamicClaims.Claims.AddRange(claims.Select(c => new AbpDynamicClaim(claimType, c.Value)));
                else
                    dynamicClaims.Claims.Add(new AbpDynamicClaim(claimType, null));  // null = 已撤销
            }
            return dynamicClaims;
        });
}
```

**缓存何时失效？** Identity 模块在角色/用户/组织变更时自动清除缓存：

```
IdentityUserManager.UpdateUserAsync()      → 清除该用户缓存
IdentityRoleManager 角色变更               → 批量清除相关用户缓存
OrganizationUnitManager 组织变更           → 批量清除成员缓存
用户登录/注册时                            → 强制清除，下次重新查 DB
```

这意味着：管理员改了张三的角色 → Identity 模块自动删 Redis 里张三的缓存 → 张三下次请求到来，缓存 miss → 重新查 DB → 发现没有 Admin 角色 → 内存里 Admin 被删掉 → 403。

#### 默认动态刷新的 Claim 类型

并非所有 claims 都会被动态刷新，只有配置在 `DynamicClaims` 列表里的才会：

```csharp
// AbpClaimsPrincipalFactoryOptions.cs
DynamicClaims = new List<string>
{
    AbpClaimTypes.UserName,
    AbpClaimTypes.Name,
    AbpClaimTypes.SurName,
    AbpClaimTypes.Role,         // ← 角色
    AbpClaimTypes.Email,
    AbpClaimTypes.EmailVerified,
    AbpClaimTypes.PhoneNumber,
    AbpClaimTypes.PhoneNumberVerified
};
```

JWT 里其他自定义的 claims（如 `jti`、`iss`、`aud`）不会被动态刷新，保持 Token 签发时的原值。

#### 微服务下：不同服务如何共享缓存

在单体应用里，Auth Server、API Server 是同一个进程，都能直接访问 Identity DB，使用 `IdentityDynamicClaimsPrincipalContributor` 即可。

在微服务里，各服务不能直连用户库，ABP 提供了三种预置的 Contributor：

```
Auth Server  →  IdentityDynamicClaimsPrincipalContributor（查 DB 写 Redis）
                    ↓ 共享 Redis（相同 KeyPrefix）
API 微服务   →  WebRemoteDynamicClaimsPrincipalContributor
                    ↓ 先读 Redis
                    └─ miss → POST /api/account/dynamic-claims/refresh（带 JWT）
                              → Auth Server 查 DB 写 Redis
                              → 微服务再读 Redis
Web UI(Tiered)→  RemoteDynamicClaimsPrincipalContributor（同上，但用 Cookie）
```

各微服务不直连用户库，只通过共享 Redis 拿数据，Auth Server 是唯一的权威数据源。

---

### 1.5 为什么这种方式更好

| 对比维度 | 短 Token | Token 黑名单 | 每请求查 DB | **ABP 动态 Claims** |
|--------|---------|------------|-----------|-------------------|
| 权限变更时效 | 几分钟 | 即时 | 即时 | **即时**（缓存清除后下次请求） |
| DB 压力 | 低（续期时才查） | 高（每次查黑名单） | **极高**（每次查） | **低**（Redis 命中不查 DB） |
| JWT 无状态优势 | 保留 | 破坏 | 破坏 | **保留**（只改内存） |
| 多服务一致性 | 难 | 需要同步 | 各自查，难一致 | **共享 Redis，天然一致** |
| 旧 claims 残留 | 有 | 整个 Token 失效 | 有（只加不删） | **无**（RemoveAll + Add） |
| 工程复杂度 | 低 | 中 | 低 | 中（框架封装，使用简单） |

ABP 的方案本质上是用「共享 Redis + 缓存失效联动」替代了「每请求查 DB」，在保持即时性的同时大幅降低了数据库压力，并且与 ASP.NET Core 认证管道深度集成，不会有旧 claims 残留的问题。

---

## 第二部分：Session 校验 —— 强制踢人即时生效

### 2.1 痛点

动态 Claims 解决了「权限变更滞后」，但还有一个更紧迫的场景没有覆盖：

**强制让某个登录会话立刻失效。**

```
场景1：安全事件 —— 运维发现张三的账号在异地登录，判断可能是账号被盗，
       需要立刻让张三当前所有 Session 失效，强制重新登录。

场景2：异常操作 —— 张三做了违规操作，管理员需要立刻踢他下线，
       不允许等到 Token 自然过期。

场景3：密码修改 —— 张三修改了密码，旧 Token 应该立刻失效。
```

动态 Claims 机制能刷新权限，但它的前提是「用户还有登录记录」。它并不检查「这个 Session 是否还被允许存在」。

即使 Dynamic Claims 把角色刷成空了，用户仍然可以访问不需要角色的接口；而且如果 Token 还有效，用户甚至可以 Refresh 拿新 Token，继续访问系统。

所以需要一个更根本的机制：**从 Token 签发的那一刻起，在数据库里登记这个 Session；每次请求都确认这个 Session 还在；管理员可以随时删除这个 Session 记录，下一次请求时立刻失效**。

---

### 2.2 业界常见方案对比

#### 方案 A：SecurityStamp（ASP.NET Core Identity 内置）

每次用户关键信息变更（密码、角色等），就更新数据库里的 `SecurityStamp` 字段。Cookie 认证会定期（默认 30 分钟）重新验证 Stamp 是否匹配，不匹配就登出。

```
优点：ASP.NET Core 内置，无需额外开发
缺点：
  - 只适用于 Cookie 认证，JWT 无效
  - 默认 30 分钟才验证一次，不够即时
  - 每次验证都查数据库
  - 无法精细控制某个特定的 Session（撤销张三的某一台设备登录）
```

#### 方案 B：短 Token + 撤销 Refresh Token

把 Refresh Token 存在数据库，强制踢人时从库里删掉。下次续期时找不到 Refresh Token，无法续期，Token 到期后自然失效。

```
优点：实现相对简单
缺点：
  - 不够即时，Access Token 有效期内无法撤销
  - 把撤销的时间窗口压缩到 Access Token 有效期（5~15 分钟）
  - 无法精确记录每个 Session 的设备信息、IP、最后访问时间
```

#### 方案 C：Token Introspection

每次请求都向授权服务器发起 HTTP 请求，询问「这个 Token 还有效吗」。

```
优点：绝对即时
缺点：
  - 每次请求都发一次 HTTP 请求，延迟和性能代价极高
  - 授权服务器成为所有请求的瓶颈
  - 微服务场景下更是灾难
```

#### 方案 D：自己维护 Session 表

在数据库里建一张 Session 表，每次请求查一下 Session 还在不在。

```
优点：可以精细控制
缺点：
  - 每请求都查 DB，数据库压力大
  - 需要自己维护缓存、过期清理、多服务一致性
  - 与认证框架耦合，实现复杂
```

ABP Pro 实现的本质是方案 D，但通过 Redis 缓存和 Dynamic Claims 管道，把它做得优雅、高效、与框架深度集成。

---

### 2.3 ABP Pro 的解法

ABP Pro 在 `Volo.Abp.Identity.Pro` 中实现了一套完整的 Session 管理机制，它的核心设计是：

**把 Session 校验寄生进 Dynamic Claims 管道，作为一个 Contributor 运行。**

这个选择非常关键，后面会详细解释为什么。

整体流程分为三个阶段：

**登录时**：生成 `sessionId`，写进 Token，同时写一条记录到数据库

**每次请求**：Dynamic Claims 管道里，取出 Token 里的 `sessionId`，查 Redis（miss 再查 DB），确认 Session 还在

**踢人时**：从 `AbpIdentitySessions` 表删掉那条记录，同时清除 Redis 缓存；下次请求时，查不到 Session → 身份置空 → 中间件 `SignOutAsync` → 401

---

### 2.4 源码逐层解析

#### 第一层：登录时创建 Session 记录

`OpenIddictCreateIdentitySession` 挂载在 OpenIddict 的 `ProcessSignInContext` 管道里，用户成功登录（Token 签发完成）时触发：

```csharp
// OpenIddictCreateIdentitySession.cs
public async ValueTask HandleAsync(OpenIddictServerEvents.ProcessSignInContext context)
{
    // client_credentials 流程（机器对机器）不创建 Session
    if (context.Request.IsClientCredentialsGrantType()) return;

    var sessionId = context.Principal.FindSessionId();  // 取出刚才写入 claims 的 sessionId

    await IdentitySessionManager.CreateAsync(
        sessionId,
        device,                                // Web / Mobile / OAuth
        WebClientInfoProvider.DeviceInfo,      // User-Agent
        context.Principal.FindUserId()!.Value,
        context.Principal.FindTenantId(),
        context.ClientId,
        WebClientInfoProvider.ClientIpAddress  // 登录 IP
    );
    // 一条 IdentitySession 记录就写入 AbpIdentitySessions 表了
}
```

那么 `sessionId` 是什么时候写进 claims 的？是在静态 Contributor 里（登录时，Token 签发前）：

```csharp
// IdentitySessionClaimsPrincipalContributor.cs（静态 Contributor，登录时运行）
public Task ContributeAsync(AbpClaimsPrincipalContributorContext context)
{
    var identity = context.ClaimsPrincipal.Identities.FirstOrDefault();
    var sessionId = identity.FindSessionId();
    if (sessionId == null)
    {
        // 还没有 sessionId，生成一个 GUID 写入 Token claims
        identity.AddClaim(new Claim(AbpClaimTypes.SessionId, Guid.NewGuid().ToString()));
    }
    return Task.CompletedTask;
}
```

这样，每次登录都会生成一个唯一的 `sessionId` GUID，写入 JWT，同时在 DB 里有对应的记录。

#### 第二层：每次请求校验 Session

`IdentitySessionDynamicClaimsPrincipalContributor` 是一个动态 Contributor，在每次请求的 Dynamic Claims 管道里运行：

```csharp
// IdentitySessionDynamicClaimsPrincipalContributor.cs
public async override Task ContributeAsync(AbpClaimsPrincipalContributorContext context)
{
    var identity = context.ClaimsPrincipal.Identities.FirstOrDefault();
    var userId = identity.FindUserId();

    var logout = false;

    var sessionId = identity.FindSessionId();  // 从 JWT claims 里取 sessionId
    if (sessionId == null)
    {
        // Token 里没有 sessionId（老版本 Token 或异常情况）→ 强制踢出
        logout = true;
    }
    else
    {
        var identitySessionChecker = context.ServiceProvider.GetRequiredService<IdentitySessionChecker>();
        if (!await identitySessionChecker.IsValidateAsync(sessionId))  // 查 Redis/DB
        {
            // Session 不存在（被删除/过期）→ 踢出
            logout = true;
        }
    }

    if (logout)
    {
        // 清空 dynamic claims 缓存（触发下次重查 DB）
        await IdentityDynamicClaimsPrincipalContributorCache.ClearAsync(userId.Value);
        
        // 把 ClaimsPrincipal 置为空身份（IsAuthenticated = false）
        context.ClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        // 设置 401 响应的错误信息
        var tokenUnauthorizedErrorInfo = context.ServiceProvider.GetRequiredService<AbpAspNetCoreTokenUnauthorizedErrorInfo>();
        tokenUnauthorizedErrorInfo.Error = AbpExceptionHandlingConsts.InvalidToken;
        tokenUnauthorizedErrorInfo.ErrorDescription = AbpExceptionHandlingConsts.SessionExpired;
    }
}
```

当 `context.ClaimsPrincipal` 被置为空身份后，回到 `AbpDynamicClaimsMiddleware`，它会检测到 `IsAuthenticated == false`，然后调用 `SignOutAsync`，整个请求链路以 401 结束。

#### 第三层：`IdentitySessionChecker` —— 核心校验逻辑

```csharp
// IdentitySessionChecker.cs
public virtual async Task<bool> IsValidateAsync(string sessionId)
{
    // 前提：必须开启 IsDynamicClaimsEnabled，否则直接放行（向后兼容）
    if (!AbpClaimsPrincipalFactoryOptions.Value.IsDynamicClaimsEnabled)
    {
        return true;
    }

    // 先查 Redis
    var sessionCacheItem = await Cache.GetOrAddAsync(sessionId, async () =>
    {
        // Redis miss → 查数据库
        var session = await IdentitySessionManager.FindAsync(sessionId, false);
        if (session == null)
        {
            return null;  // DB 里也没有 → 说明已被删除或从未存在
        }
        return new IdentitySessionCacheItem { Id = session.Id, SessionId = session.SessionId };
    });

    if (sessionCacheItem == null)
    {
        // Redis 和 DB 都没有 → Session 已失效
        await Cache.RemoveAsync(sessionId);
        return false;  // ← 触发踢人
    }

    // Session 有效 → 更新访问记录
    sessionCacheItem.CacheLastAccessed = Clock.Now;
    sessionCacheItem.IpAddress = WebClientInfoProvider.ClientIpAddress;
    sessionCacheItem.HitCount++;

    // 不是每次都写 DB，每命中 10 次（可配置）才更新 DB 里的 LastAccessed
    if (sessionCacheItem.HitCount == 1 || sessionCacheItem.HitCount > Options.Value.UpdateSessionAfterCacheHit)
    {
        sessionCacheItem.HitCount = 0;
        await IdentitySessionManager.UpdateSessionFromCacheAsync(sessionId, sessionCacheItem);
    }

    await Cache.SetAsync(sessionId, sessionCacheItem);  // 更新 Redis
    return true;
}
```

**性能优化细节**：`UpdateSessionAfterCacheHit` 默认是 10，意思是每 10 次请求才把 `LastAccessed` 写回 DB 一次。在高频访问场景下，这个参数把 DB 写操作压缩了 90%，只用 Redis 做高频读写。

---

### 2.5 为什么这种方式更好

#### 为什么把 Session 校验做成 Dynamic Claims Contributor？

这是 ABP 设计中最精妙的一点。Session 校验没有单独实现一个中间件，而是作为 Dynamic Claims 管道中的一个 Contributor。好处是：

1. **复用管道**：中间件只需要一个（`AbpDynamicClaimsMiddleware`），Session 校验和权限刷新都在同一个管道里，请求处理链路清晰
2. **自动协作**：Session 失效时，Contributor 把 `ClaimsPrincipal` 置空，中间件统一处理登出逻辑，不需要 Contributor 自己调 `SignOutAsync`
3. **条件绑定**：Session 校验强依赖 `IsDynamicClaimsEnabled`，如果动态 Claims 没开，Session 校验也自动跳过，不会有兼容性问题

#### 与其他方案对比

| 对比维度 | SecurityStamp | 短 Token+Revoke | Token Introspection | **ABP Session Checker** |
|--------|-------------|---------------|-------------------|----------------------|
| 适用认证方式 | Cookie only | Cookie + JWT | JWT | **Cookie + JWT 均可** |
| 踢人时效 | 30分钟 | Access Token 到期 | 即时 | **即时（下次请求）** |
| 每次请求 DB 压力 | 每 30min 查一次 | 无 | 每次查 | **Redis 命中时不查 DB** |
| 精细到单 Session | 不支持 | Refresh Token 粒度 | 可以 | **支持（按 sessionId）** |
| Session 信息记录 | 无 | 无 | 无 | **有（设备、IP、最后访问）** |
| 工程复杂度 | 低 | 中 | 高 | 中（框架封装） |

ABP 的方案在「即时性」、「性能」、「精细控制」三个维度上取得了最好的平衡。

---

## 三、两套机制的关系与协作

这两套机制都寄生在同一条管道里，但职责完全不同：

```
UseAuthentication
    └─ 解析 JWT → ClaimsPrincipal (内存，含旧权限 + sessionId)

UseDynamicClaims（AbpDynamicClaimsMiddleware）
    └─ CreateDynamicAsync(context.User)
         ├─ [Contributor 1] IdentityDynamicClaimsPrincipalContributor
         │    └─ 从 Redis/DB 取最新角色权限，原地覆写 ClaimsPrincipal
         │         ↑ 解决「权限变更滞后」问题
         │
         └─ [Contributor 2] IdentitySessionDynamicClaimsPrincipalContributor
              └─ 从 JWT 取 sessionId，查 Redis/DB 确认 Session 存在
                   ├─ 存在 → 更新访问记录，放行
                   └─ 不存在 → ClaimsPrincipal = 空身份
                                    ↓
                                中间件检测 IsAuthenticated == false
                                    ↓
                                SignOutAsync → 401
                         ↑ 解决「强制踢人」问题

UseAuthorization
    └─ 基于刷新后的 HttpContext.User 做授权检查
```

两个 Contributor 是顺序执行的。如果 Session 已经失效（Contributor 2 把 Principal 置空），那 Contributor 1 的权限刷新实际上也没有意义了——因为整个身份已经被清空，不管什么权限都不重要了。

---

## 四、总结

| 机制 | 解决的问题 | 核心手段 | 触发时机 |
|------|---------|---------|---------|
| 动态 Claims | 权限变更滞后（JWT 里的 role 已过时） | 每请求从 Redis/DB 覆写内存里的 ClaimsPrincipal | `UseDynamicClaims()` 中间件，每次请求 |
| Session 校验 | 无法强制踢人（被撤销的 Session 仍有效） | JWT 里携带 sessionId，每请求查 Redis/DB 确认 Session 存在 | Dynamic Claims Contributor，每次请求 |

**两套机制统一在一个前提下：`IsDynamicClaimsEnabled = true`**。关闭这个开关，两个机制都会失效（Session Checker 里有明确的 early return 判断）。

从工程角度看，ABP 的设计有几个值得学习的点：

1. **内存原地改写而不是重建 Token**：避免了 Token 签发链路的改造，与现有认证框架无缝集成
2. **Redis 缓存 + 失效联动**：把「每请求查 DB」变成「每请求查缓存，写操作时主动清缓存」，是经典的读多写少优化
3. **Contributor 模式**：功能扩展不需要改中间件，只需要注册新的 Contributor，开放封闭原则的典范
4. **Session 校验寄生进 Claims 管道**：两种不同性质的功能（权限刷新 + Session 校验）共用同一条管道，职责清晰，代码量少

---

*基于 ABP Framework 10.x 源码分析，Pro 版本功能需要 Volo.Abp.Identity.Pro 许可。*
