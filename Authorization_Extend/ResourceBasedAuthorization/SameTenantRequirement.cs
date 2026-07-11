using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 场景一「多租户隔离」的要求：要求操作的订单必须属于当前用户所在租户。
/// 空标记，真正的比对逻辑在 SameTenantAuthorizationHandler 里。
/// </summary>
public class SameTenantRequirement : IAuthorizationRequirement
{
}
