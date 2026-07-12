namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 标记「有明确归属人」的业务资源。订单、合同、工单等实体实现此接口后，
/// 即可共用 <see cref="OwnedResourceAuthorizationHandler"/>，无需每表写一个 Handler。
/// </summary>
public interface IOwnedResource
{
    /// <summary>资源归属用户 ID（通常对应创建者 / 负责人）。</summary>
    string OwnerUserId { get; }
}
