namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>
/// 敏感字段名常量。与登录时写入的 FieldPermission Claim 值一一对应，
/// 避免 Handler / Service 里散落魔法字符串。
/// </summary>
public static class FieldNames
{
    /// <summary>基本工资 —— 普通员工默认可看自己的。</summary>
    public const string BaseSalary = "BaseSalary";

    /// <summary>绩效奖金 —— 仅 HR 等持有该字段权限的人可看。</summary>
    public const string Bonus = "Bonus";

    /// <summary>社保明细 —— 高度敏感，仅 HR 可看。</summary>
    public const string SocialSecurity = "SocialSecurity";
}
