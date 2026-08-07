namespace Custom2FA_Demo.Domain;

/// <summary>
/// 对应 Identity 的 IdentityUser（精简版）。
/// AuthenticatorKey / RecoveryCodes 不在本表，而在 UserTokens（对齐 AspNetUserTokens）。
/// </summary>
public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";

    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneConfirmed { get; set; }

    /// <summary>总开关。为 true 且 TwoFactorMethods!=0 时登录走第二段。</summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// 用户勾选的 2FA 方式位标志：1=Authenticator, 2=Sms, 4=Email，全选=7。
    /// 登录时返回「已勾选且实际可用来」的列表给前端选择。
    /// </summary>
    public TwoFactorMethods TwoFactorMethods { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // —— 以下由 UserTokens 加载，不落 Users 表 ——
    public string? AuthenticatorKey { get; set; }
    public string? RecoveryCodes { get; set; }
}
