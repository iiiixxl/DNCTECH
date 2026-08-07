using Custom2FA_Demo.Domain;

namespace Custom2FA_Demo.Abstractions;

/// <summary>对应 Identity 的 IUserStore / UserManager 中「找用户、持久化」部分。</summary>
public interface IUserStore
{
    Task<AppUser?> FindByNameAsync(string userName);
    Task<AppUser?> FindByIdAsync(Guid id);
    Task CreateAsync(AppUser user);
    Task UpdateAsync(AppUser user);
}

/// <summary>对应 Identity AspNetUserTokens 的读写。</summary>
public interface IUserTokenStore
{
    Task<string?> GetTokenAsync(Guid userId, string loginProvider, string name);
    Task SetTokenAsync(Guid userId, string loginProvider, string name, string? value);
    Task RemoveTokenAsync(Guid userId, string loginProvider, string name);
}

/// <summary>对应 IPasswordHasher&lt;TUser&gt;。</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

/// <summary>
/// 对应 IUserClaimsPrincipalFactory：用户 → ClaimsPrincipal。
/// JWT 场景：登录签发前调用；请求时从 token 还原，不再调用。
/// </summary>
public interface IUserClaimsPrincipalFactory
{
    System.Security.Claims.ClaimsPrincipal Create(AppUser user, string authenticationType, IEnumerable<System.Security.Claims.Claim>? extra = null);
}

/// <summary>对应 AuthenticatorTokenProvider（TOTP）。</summary>
public interface ITotpService
{
    string GenerateKey();
    string BuildOtpAuthUri(string issuer, string accountName, string key);
    bool Validate(string key, string code);
}

/// <summary>
/// 对应 SignInManager 两段式里的「临时 2FA 态」。
/// Identity 用 Cookie(TwoFactorUserIdScheme)；这里用短命 mfa_ticket JWT。
/// </summary>
public interface IMfaTicketService
{
    string CreateTicket(Guid userId);
    Guid? ValidateTicket(string ticket);
}

/// <summary>正式 access token（对应 Cookie SignIn 或你们用户中心的 JWT）。</summary>
public interface IAccessTokenService
{
    string CreateAccessToken(System.Security.Claims.ClaimsPrincipal principal);
}
