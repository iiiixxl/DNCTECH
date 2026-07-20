using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 数据所有权要求：当前用户必须是资源的归属人，否则不能编辑 / 删除 / 操作。
/// 适用于所有实现了 <see cref="IOwnedResource"/> 的实体。
/// </summary>
public class OwnedResourceRequirement : IAuthorizationRequirement
{
}
