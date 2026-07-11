using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>
/// 字段级访问要求（诉求单）：携带「要访问哪个字段」。
/// 本身不干活，由 FieldAccessHandler 根据用户的 FieldPermission Claim 决定是否 Succeed。
/// </summary>
public class FieldAccessRequirement : IAuthorizationRequirement
{
    public FieldAccessRequirement(string fieldName)
    {
        FieldName = fieldName;
    }

    /// <summary>目标字段名，如 Bonus、SocialSecurity。</summary>
    public string FieldName { get; }
}
