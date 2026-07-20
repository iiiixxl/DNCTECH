using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace Authentication_jwt_cookie.Services;

/// <summary>
/// Cookie 认证的服务端会话存储（Redis 版，适合多实例生产部署）。
/// </summary>
/// <remarks>
/// 启用步骤：
/// 1. 安装 NuGet 包：Microsoft.Extensions.Caching.StackExchangeRedis
/// 2. 注册 Redis 缓存：
///    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
/// 3. 在 ClassicDemoAuthenticationExtensions 中将 ITicketStore 改为 RedisTicketStore
/// </remarks>
public class RedisTicketStore : ITicketStore
{
    private readonly IDistributedCache _cache;
    private const string KeyPrefix = "auth-session:";

    public RedisTicketStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var sessionId = KeyPrefix + Guid.NewGuid().ToString("N");
        await RenewAsync(sessionId, ticket);
        return sessionId;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var options = new DistributedCacheEntryOptions();
        if (ticket.Properties.ExpiresUtc is { } expiresUtc)
        {
            options.AbsoluteExpiration = expiresUtc;
        }

        // TicketSerializer 将 AuthenticationTicket 序列化为字节数组存入 Redis
        var bytes = TicketSerializer.Default.Serialize(ticket);
        await _cache.SetAsync(key, bytes, options);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var bytes = await _cache.GetAsync(key);
        return bytes is null ? null : TicketSerializer.Default.Deserialize(bytes);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}
