namespace Custom2FA_Demo.Models;

public record RegisterRequest(string UserName, string Password, string? Email = null, string? Phone = null);
public record LoginRequest(string UserName, string Password);
public record TwoFactorRequest(string MfaTicket, string Provider, string Code);
public record RecoveryRequest(string MfaTicket, string RecoveryCode);
public record Enable2FaRequest(string Code);

/// <summary>用 class + 属性，避免 positional record 在部分绑定场景下 Methods 恒为 0。</summary>
public class SetMethodsRequest
{
    public int Methods { get; set; }
}

public record ContactRequest(string? Email, bool EmailConfirmed, string? Phone, bool PhoneConfirmed);
