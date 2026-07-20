using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>
/// 员工档案访问要求：先判断调用者能不能查看这一条员工记录，
/// 通过后才进入字段级裁剪。
/// </summary>
public sealed class EmployeeProfileAccessRequirement : IAuthorizationRequirement
{
}
