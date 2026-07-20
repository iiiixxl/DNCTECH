using System.Text.Json.Serialization;

namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>
/// 对外返回的员工薪资 DTO。
/// 敏感字段用可空类型：无权限时保持 null，前端据此隐藏列，而不是返回 0 误导业务。
/// 同时作为资源型授权的 TResource，交给 FieldAccessHandler 做字段级判断。
/// </summary>
public class EmployeeDto
{
    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>基本工资。有 BaseSalary 字段权限时才赋值。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? BaseSalary { get; set; }

    /// <summary>绩效奖金。有 Bonus 字段权限时才赋值；否则不写入响应。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Bonus { get; set; }

    /// <summary>社保明细。有 SocialSecurity 字段权限时才赋值。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SocialSecurityDetail { get; set; }

    /// <summary>本次响应实际暴露了哪些字段（便于联调观察，生产可去掉）。</summary>
    public IReadOnlyList<string> VisibleFields { get; set; } = [];
}
