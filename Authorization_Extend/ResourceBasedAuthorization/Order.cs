namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 演示用「资源」对象：订单。
/// 基于资源的授权，判断的不是「你能不能退款」这种功能权限，
/// 而是「你能不能退『这一张』订单」——所以必须把具体资源实例交给 Handler 去比对。
/// </summary>
public class Order : ITenantOwnedResource
{
    public int Id { get; init; }

    /// <summary>所属租户（门店）ID。多租户隔离：用户只能操作自己租户的订单。</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>创建者用户 ID。数据所有权：用户只能操作自己创建的订单。</summary>
    public string OwnerUserId { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Description { get; init; } = string.Empty;
}
