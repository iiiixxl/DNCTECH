namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>
/// 演示用「员工薪资」实体（模拟数据库行）。
/// 含公开字段与敏感字段；敏感字段是否出现在 API 响应里，由字段级授权决定。
/// </summary>
public class Employee
{
    public string UserId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary>基本工资 —— 相对低敏，员工本人默认可看。</summary>
    public decimal BaseSalary { get; init; }

    /// <summary>绩效奖金 —— 高敏，仅持有 Bonus 字段权限者可看。</summary>
    public decimal Bonus { get; init; }

    /// <summary>社保明细 —— 高敏，仅持有 SocialSecurity 字段权限者可看。</summary>
    public string SocialSecurityDetail { get; init; } = string.Empty;
}
