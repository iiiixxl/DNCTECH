namespace Custom2FA_Demo.Domain;

/// <summary>
/// 用户启用的二次认证方式（位运算）。
/// Authenticator=1, Sms=2, Email=4；三种全开 = 7。
/// </summary>
[Flags]
public enum TwoFactorMethods
{
    None = 0,
    Authenticator = 1,
    Sms = 2,
    Email = 4
}

public static class TwoFactorMethodNames
{
    public const string Authenticator = "Authenticator";
    public const string Sms = "Sms";
    public const string Email = "Email";

    public static string ToProviderName(TwoFactorMethods method) => method switch
    {
        TwoFactorMethods.Authenticator => Authenticator,
        TwoFactorMethods.Sms => Sms,
        TwoFactorMethods.Email => Email,
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    public static TwoFactorMethods? FromProviderName(string name) => name switch
    {
        Authenticator => TwoFactorMethods.Authenticator,
        Sms => TwoFactorMethods.Sms,
        Email => TwoFactorMethods.Email,
        _ => null
    };
}

/// <summary>对应 Identity AspNetUserTokens 的一行。</summary>
public class UserToken
{
    public Guid UserId { get; set; }
    public string LoginProvider { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Value { get; set; }
}

public static class UserTokenNames
{
    /// <summary>与 Identity UserStoreBase.InternalLoginProvider 一致的教学命名。</summary>
    public const string InternalLoginProvider = "[AspNetUserStore]";
    public const string AuthenticatorKey = "AuthenticatorKey";
    public const string RecoveryCodes = "RecoveryCodes";
}
