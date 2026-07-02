using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Authentication_jwt_cookie.Services;

/// <summary>
/// Cookie 认证的服务端会话存储（内存版，适合单机 Demo）。
/// </summary>
/// <remarks>
/// 工作原理：
/// 1. 登录时 ASP.NET Core 调用 <see cref="StoreAsync"/>，生成 SessionId 写入 Cookie
/// 2. 完整的 AuthenticationTicket 保存在 IMemoryCache 中
/// 3. 后续请求通过 SessionId 从缓存取回 Ticket，完成身份验证
/// 4. 登出时调用 <see cref="RemoveAsync"/> 删除服务端会话
///
/// 多实例部署时内存不共享，请改用 <see cref="RedisTicketStore"/>。
/// </remarks>
public class MemoryTicketStore : ITicketStore
{
    private readonly IMemoryCache _cache;

    /// <summary>缓存键前缀，避免与其他缓存项冲突。</summary>
    private const string KeyPrefix = "auth-session:";

    public MemoryTicketStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var sessionId = KeyPrefix + Guid.NewGuid().ToString("N");
        await RenewAsync(sessionId, ticket);
        return sessionId;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        // 与会话过期时间对齐，避免 Cookie 有效但 Ticket 已被提前清除
        var expires = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddHours(8);
        _cache.Set(key, ticket, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expires
        });
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        _cache.TryGetValue(key, out AuthenticationTicket? ticket);
        return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
