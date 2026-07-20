namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 演示：合同与订单共用同一套 <see cref="OwnedResourceAuthorizationHandler"/>，
/// 新实体只需实现 <see cref="IOwnedResource"/>，不必再写 Handler。
/// </summary>
public class Contract : ITenantOwnedResource
{
    public int Id { get; init; }

    public string TenantId { get; init; } = string.Empty;

    public string OwnerUserId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
}
