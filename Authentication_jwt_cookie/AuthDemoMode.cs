namespace Authentication_jwt_cookie;

/// <summary>
/// 启动时选择哪一套认证 Demo（在 Program.cs 里改这一处即可）。
/// </summary>
public enum AuthDemoMode
{
    /// <summary>原 Demo：Cookie + JWT（Controllers/Auth、JwtAuth、User、Product）</summary>
    Classic = 0,

    /// <summary>DynamicClaims Demo：JWT + session_id（DynamicClaims/）</summary>
    DynamicSession = 1
}
