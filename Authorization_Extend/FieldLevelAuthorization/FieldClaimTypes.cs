namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>
/// 字段级权限相关的 Claim 类型。
/// 登录时按用户身份写入多条 FieldPermission Claim（一条 Claim = 一个可访问字段）。
/// </summary>
public static class FieldClaimTypes
{
    /// <summary>字段权限声明。Value 为字段名，如 BaseSalary / Bonus / SocialSecurity。</summary>
    public const string FieldPermission = "FieldPermission";
}
