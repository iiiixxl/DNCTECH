using Custom2FA_Demo.Abstractions;
using OtpNet;

namespace Custom2FA_Demo.Infrastructure;

/// <summary>对应 Identity AuthenticatorTokenProvider（Rfc6238 TOTP）。</summary>
public sealed class TotpService : ITotpService
{
    public string GenerateKey()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string BuildOtpAuthUri(string issuer, string accountName, string key)
        => $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

    public bool Validate(string key, string code)
    {
        code = code.Replace(" ", "").Replace("-", "");
        if (!int.TryParse(code, out _)) return false;
        var bytes = Base32Encoding.ToBytes(key);
        var totp = new Totp(bytes);
        // 与 Identity 类似：允许前后时间窗
        return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 2, future: 2));
    }
}
