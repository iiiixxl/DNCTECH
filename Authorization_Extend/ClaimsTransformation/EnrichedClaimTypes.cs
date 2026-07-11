namespace Authorization_Extend.ClaimsTransformation;

/// <summary>
/// 声明转换补充的 Claim 类型常量。
/// </summary>
public static class EnrichedClaimTypes
{
    /// <summary>部门。</summary>
    public const string Department = "department";

    /// <summary>权限点。</summary>
    public const string Permission = "permission";

    /// <summary>
    /// 幂等哨兵：标记「本 identity 已被转换过」。
    /// TransformAsync 每次请求都会被调用，靠它避免重复塞 Claim。
    /// </summary>
    public const string Enriched = "claims_enriched";
}
