namespace Authorization_Extend.TempPermissionAuthorization;

/// <summary>
/// 临时权限相关的 Claim 类型常量。
/// 主管授权时写入 Token/Cookie；声明转换与 Handler 都认这些类型。
/// </summary>
public static class TempClaimTypes
{
    /// <summary>临时权限点，如 expense.approve。</summary>
    public const string Permission = "TempPermission";

    /// <summary>
    /// 临时权限失效时刻（UTC，ISO-8601）。
    /// 与 TempPermission 成对出现：过期后转换器会一并摘掉。
    /// </summary>
    public const string ValidUntil = "TempValidUntil";

    /// <summary>
    /// 幂等哨兵：标记「本 identity 已做过临时权限转换」，避免 TransformAsync 重复注入。
    /// </summary>
    public const string Enriched = "temp_permission_enriched";
}
