using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>
/// 员工薪资查询服务：在数据访问 / 组装 DTO 时注入 IAuthorizationService，
/// 按字段逐个 AuthorizeAsync，有权限才赋值 —— 权限过滤留在授权层，业务层不写死 if(role)。
/// </summary>
public class EmployeeService
{
    private readonly IEmployeeStore _store;
    private readonly IAuthorizationService _authorizationService;

    public EmployeeService(IEmployeeStore store, IAuthorizationService authorizationService)
    {
        _store = store;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// 按 userId 取薪资档案，并根据当前用户的字段权限动态裁剪敏感列。
    /// </summary>
    public async Task<EmployeeDto?> GetProfileAsync(ClaimsPrincipal user, string userId)
    {
        var employee = _store.Find(userId);
        if (employee is null)
        {
            return null;
        }

        // 先组装「公开」部分；敏感字段默认不填，等授权通过再赋值
        var dto = new EmployeeDto
        {
            UserId = employee.UserId,
            Name = employee.Name
        };

        var visible = new List<string> { nameof(EmployeeDto.Name) };

        // 基本工资：普通员工通常有此字段权限
        if (await CanAccessFieldAsync(user, dto, FieldNames.BaseSalary))
        {
            dto.BaseSalary = employee.BaseSalary;
            visible.Add(FieldNames.BaseSalary);
        }

        // 绩效奖金：仅 HR 等持有 Bonus 字段权限者可见
        if (await CanAccessFieldAsync(user, dto, FieldNames.Bonus))
        {
            dto.Bonus = employee.Bonus;
            visible.Add(FieldNames.Bonus);
        }

        // 社保明细：高度敏感
        if (await CanAccessFieldAsync(user, dto, FieldNames.SocialSecurity))
        {
            dto.SocialSecurityDetail = employee.SocialSecurityDetail;
            visible.Add(FieldNames.SocialSecurity);
        }

        dto.VisibleFields = visible;
        return dto;
    }

    /// <summary>
    /// 命令式字段授权：把 DTO 当资源、字段名当 Requirement，交给 FieldAccessHandler。
    /// </summary>
    private Task<bool> CanAccessFieldAsync(ClaimsPrincipal user, EmployeeDto dto, string fieldName)
    {
        return AuthorizeFieldAsync(user, dto, fieldName);
    }

    private async Task<bool> AuthorizeFieldAsync(ClaimsPrincipal user, EmployeeDto dto, string fieldName)
    {
        // 关键：传入具体 Requirement 实例（不是策略名），框架会路由到
        // AuthorizationHandler&lt;FieldAccessRequirement, EmployeeDto&gt;
        var result = await _authorizationService.AuthorizeAsync(
            user,
            dto,
            new FieldAccessRequirement(fieldName));

        return result.Succeeded;
    }
}
