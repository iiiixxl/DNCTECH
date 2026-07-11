namespace Authorization_Extend.TempPermissionAuthorization;

/// <summary>
/// 一条临时授权记录：谁被授予、授予什么权限、何时失效、谁授的。
/// 真实项目对应 temp_permission_grants 表；本 Demo 用内存字典模拟。
/// </summary>
public class TempPermissionGrant
{
    /// <summary>被授权人 userId（如 user-normal）。</summary>
    public required string GranteeUserId { get; init; }

    /// <summary>临时权限编码（如 expense.approve）。</summary>
    public required string Permission { get; init; }

    /// <summary>失效时刻（UTC）。</summary>
    public required DateTimeOffset ValidUntil { get; init; }

    /// <summary>授权人 userId（主管）。</summary>
    public required string GrantedByUserId { get; init; }

    /// <summary>授权时间（UTC）。</summary>
    public DateTimeOffset GrantedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>是否仍在有效期内。</summary>
    public bool IsActive => ValidUntil > DateTimeOffset.UtcNow;
}
