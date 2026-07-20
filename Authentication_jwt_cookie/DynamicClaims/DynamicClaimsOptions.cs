namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>动态 session 开关。对应 ABP <c>IsDynamicClaimsEnabled</c>。</summary>
public class DynamicClaimsOptions
{
    public bool IsEnabled { get; set; } = true;
}
