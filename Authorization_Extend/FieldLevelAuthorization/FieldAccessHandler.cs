using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>
/// 字段级授权裁判：资源型 Handler，泛型第二参数是 EmployeeDto。
/// 从当前用户的 FieldPermission Claim 列表里查，是否包含 Requirement 要求的字段名。
/// 有 → Succeed；没有 → 不 Succeed（调用方据此跳过该字段赋值）。
/// </summary>
/// <remarks>
/// 与「整接口 403」不同：字段级授权失败时，业务层通常只是「不填这个字段」，
/// 接口整体仍返回 200，只是敏感列为空。这是字段级授权和功能/资源授权的关键差异。
/// </remarks>
public class FieldAccessHandler : AuthorizationHandler<FieldAccessRequirement, EmployeeDto>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FieldAccessRequirement requirement,
        EmployeeDto resource)
    {
        // 登录时写入的多条 FieldPermission Claim，Value = 字段名
        var allowedFields = context.User.Claims
            .Where(c => c.Type == FieldClaimTypes.FieldPermission)
            .Select(c => c.Value);

        if (allowedFields.Contains(requirement.FieldName, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        // resource 参数在本演示里未参与判断（字段权限跟人走，不跟某条员工记录走）。
        // 若业务要「只能看自己的奖金、不能看别人的」，可在此叠加 resource.UserId == 当前用户 的判断。
        _ = resource;

        return Task.CompletedTask;
    }
}
