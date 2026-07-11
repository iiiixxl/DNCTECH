using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 场景二「数据所有权」的要求：要求操作的订单必须是当前用户本人创建的。
/// 空标记，真正的比对逻辑在 OrderOwnerAuthorizationHandler 里。
/// </summary>
public class OrderOwnerRequirement : IAuthorizationRequirement
{
}
