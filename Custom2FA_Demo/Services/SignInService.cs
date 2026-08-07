using System.Security.Claims;
using System.Security.Cryptography;
using Custom2FA_Demo.Abstractions;
using Custom2FA_Demo.Domain;

namespace Custom2FA_Demo.Services;

public sealed record AuthResult(
    bool Succeeded,
    bool RequiresTwoFactor = false,
    string? AccessToken = null,
    string? MfaTicket = null,
    string? Error = null,
    string[]? RecoveryCodes = null,
    /// <summary>登录需要 2FA 时，返回用户可选的方式（位标志与实际可用条件的交集）。</summary>
    string[]? Providers = null,
    int TwoFactorMethods = 0);

/// <summary>
/// 对应 Identity 的 SignInManager：密码 →（可选）2FA → 正式登录。
/// </summary>
public sealed class SignInService(
    IUserStore users,
    IPasswordHasher passwordHasher,
    IUserClaimsPrincipalFactory claimsFactory,
    ITotpService totp,
    IMfaTicketService mfaTickets,
    IAccessTokenService accessTokens)
{
    public async Task<AuthResult> RegisterAsync(string userName, string password, string? email = null, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return new AuthResult(false, Error: "用户名或密码不能为空");

        if (await users.FindByNameAsync(userName) != null)
            return new AuthResult(false, Error: "用户名已存在");

        var user = new AppUser
        {
            UserName = userName.Trim(),
            PasswordHash = passwordHasher.Hash(password),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            EmailConfirmed = !string.IsNullOrWhiteSpace(email),
            PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            PhoneConfirmed = !string.IsNullOrWhiteSpace(phone)
        };
        await users.CreateAsync(user);

        var principal = claimsFactory.Create(user, "Bearer", [new Claim("amr", "pwd")]);
        return new AuthResult(true, AccessToken: accessTokens.CreateAccessToken(principal));
    }

    public async Task<AuthResult> PasswordSignInAsync(string userName, string password)
    {
        var user = await users.FindByNameAsync(userName);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
            return new AuthResult(false, Error: "用户名或密码错误");

        var providers = GetAvailableProviders(user);
        if (user.TwoFactorEnabled && providers.Count > 0)
        {
            var ticket = mfaTickets.CreateTicket(user.Id);
            return new AuthResult(
                true,
                RequiresTwoFactor: true,
                MfaTicket: ticket,
                Providers: providers.ToArray(),
                TwoFactorMethods: (int)user.TwoFactorMethods);
        }

        var principal = claimsFactory.Create(user, "Bearer", [new Claim("amr", "pwd")]);
        return new AuthResult(true, AccessToken: accessTokens.CreateAccessToken(principal));
    }

    /// <summary>第二步：必须指定 provider（Authenticator / Sms / Email）。</summary>
    public async Task<AuthResult> TwoFactorSignInAsync(string mfaTicket, string provider, string code)
    {
        var userId = mfaTickets.ValidateTicket(mfaTicket);
        if (userId is null)
            return new AuthResult(false, Error: "mfa_ticket 无效或已过期");

        var user = await users.FindByIdAsync(userId.Value);
        if (user is null)
            return new AuthResult(false, Error: "用户不存在");

        var available = GetAvailableProviders(user);
        if (!available.Contains(provider, StringComparer.OrdinalIgnoreCase))
            return new AuthResult(false, Error: $"当前不可使用提供程序 {provider}");

        var ok = provider switch
        {
            TwoFactorMethodNames.Authenticator =>
                !string.IsNullOrEmpty(user.AuthenticatorKey) && totp.Validate(user.AuthenticatorKey, code),
            // Sms/Email：Demo 仅演示通道选择；真实环境应校验 Redis 中的一次性码
            TwoFactorMethodNames.Sms or TwoFactorMethodNames.Email =>
                !string.IsNullOrWhiteSpace(code) && code.Length >= 4,
            _ => false
        };

        if (!ok)
            return new AuthResult(false, Error: "验证码无效");

        var principal = claimsFactory.Create(user, "Bearer",
        [
            new Claim("amr", "mfa"),
            new Claim("mfa_provider", provider)
        ]);
        return new AuthResult(true, AccessToken: accessTokens.CreateAccessToken(principal));
    }

    public async Task<AuthResult> RecoveryCodeSignInAsync(string mfaTicket, string recoveryCode)
    {
        var userId = mfaTickets.ValidateTicket(mfaTicket);
        if (userId is null)
            return new AuthResult(false, Error: "mfa_ticket 无效或已过期");

        var user = await users.FindByIdAsync(userId.Value);
        if (user is null || string.IsNullOrEmpty(user.RecoveryCodes))
            return new AuthResult(false, Error: "无恢复码");

        var codes = user.RecoveryCodes.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        var match = codes.FirstOrDefault(c => string.Equals(c, recoveryCode.Trim(), StringComparison.Ordinal));
        if (match is null)
            return new AuthResult(false, Error: "恢复码无效");

        codes.Remove(match);
        user.RecoveryCodes = codes.Count == 0 ? null : string.Join(';', codes);
        await users.UpdateAsync(user);

        var principal = claimsFactory.Create(user, "Bearer", [new Claim("amr", "mfa")]);
        return new AuthResult(true, AccessToken: accessTokens.CreateAccessToken(principal));
    }

    public async Task<(AuthResult Result, string? Key, string? Uri)> GetSetupInfoAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return (new AuthResult(false, Error: "用户不存在"), null, null);

        if (string.IsNullOrEmpty(user.AuthenticatorKey))
        {
            user.AuthenticatorKey = totp.GenerateKey();
            await users.UpdateAsync(user);
        }

        var uri = totp.BuildOtpAuthUri("Custom2FA_Demo", user.UserName, user.AuthenticatorKey);
        return (new AuthResult(true), user.AuthenticatorKey, uri);
    }

    public async Task<AuthResult> ConfirmEnable2FaAsync(Guid userId, string code)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null || string.IsNullOrEmpty(user.AuthenticatorKey))
            return new AuthResult(false, Error: "请先获取 setup key");

        if (!totp.Validate(user.AuthenticatorKey, code))
            return new AuthResult(false, Error: "验证码无效，无法启用 2FA");

        var recovery = Enumerable.Range(0, 5)
            .Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(4)))
            .ToArray();

        user.TwoFactorEnabled = true;
        user.TwoFactorMethods |= TwoFactorMethods.Authenticator;
        user.RecoveryCodes = string.Join(';', recovery);
        await users.UpdateAsync(user);

        return new AuthResult(true, RecoveryCodes: recovery, TwoFactorMethods: (int)user.TwoFactorMethods,
            Providers: GetAvailableProviders(user).ToArray());
    }

    /// <summary>
    /// 设置用户允许的 2FA 方式位标志（1/2/4/…/7）。
    /// 勾选值原样入库；登录时 <see cref="GetAvailableProviders"/> 再与「实际可用前提」求交。
    /// </summary>
    public async Task<AuthResult> SetTwoFactorMethodsAsync(Guid userId, TwoFactorMethods methods)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return new AuthResult(false, Error: "用户不存在");

        // 只保留合法位，不再因「尚未绑密钥 / 未确认手机邮箱」而静默清零
        var valid = TwoFactorMethods.Authenticator | TwoFactorMethods.Sms | TwoFactorMethods.Email;
        user.TwoFactorMethods = methods & valid;
        // 总开关：有任意位则视为开启（与 enable/disable 配合；disable 会清零）
        if (user.TwoFactorMethods != TwoFactorMethods.None)
            user.TwoFactorEnabled = true;
        await users.UpdateAsync(user);

        return new AuthResult(true,
            TwoFactorMethods: (int)user.TwoFactorMethods,
            Providers: GetAvailableProviders(user).ToArray());
    }

    public async Task<AuthResult> UpdateContactAsync(Guid userId, string? email, bool emailConfirmed, string? phone, bool phoneConfirmed)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return new AuthResult(false, Error: "用户不存在");

        user.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        user.EmailConfirmed = emailConfirmed && user.Email != null;
        user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        user.PhoneConfirmed = phoneConfirmed && user.PhoneNumber != null;
        await users.UpdateAsync(user);
        return new AuthResult(true);
    }

    public async Task<AuthResult> Disable2FaAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return new AuthResult(false, Error: "用户不存在");

        user.TwoFactorEnabled = false;
        user.TwoFactorMethods = TwoFactorMethods.None;
        user.AuthenticatorKey = null;
        user.RecoveryCodes = null;
        await users.UpdateAsync(user);
        return new AuthResult(true);
    }

    public async Task<object?> GetProfileAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return null;
        return new
        {
            user.UserName,
            user.Email,
            user.EmailConfirmed,
            user.PhoneNumber,
            user.PhoneConfirmed,
            user.TwoFactorEnabled,
            twoFactorMethods = (int)user.TwoFactorMethods,
            providers = GetAvailableProviders(user),
            hasAuthenticatorKey = !string.IsNullOrEmpty(user.AuthenticatorKey)
        };
    }

    /// <summary>
    /// 位标志 ∩ 实际可用前提。
    /// 例如 Methods=7 但没绑 Authenticator、手机未确认，则只返回 Email。
    /// </summary>
    public static List<string> GetAvailableProviders(AppUser user)
    {
        var list = new List<string>();
        if (!user.TwoFactorEnabled || user.TwoFactorMethods == TwoFactorMethods.None)
            return list;

        if (user.TwoFactorMethods.HasFlag(TwoFactorMethods.Authenticator)
            && !string.IsNullOrEmpty(user.AuthenticatorKey))
            list.Add(TwoFactorMethodNames.Authenticator);

        if (user.TwoFactorMethods.HasFlag(TwoFactorMethods.Sms)
            && user.PhoneConfirmed
            && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            list.Add(TwoFactorMethodNames.Sms);

        if (user.TwoFactorMethods.HasFlag(TwoFactorMethods.Email)
            && user.EmailConfirmed
            && !string.IsNullOrWhiteSpace(user.Email))
            list.Add(TwoFactorMethodNames.Email);

        return list;
    }
}
