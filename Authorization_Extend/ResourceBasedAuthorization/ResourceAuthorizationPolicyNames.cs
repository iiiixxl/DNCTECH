namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 基于资源授权的策略名常量。这些策略在注册时用 AddPolicy 显式登记，
/// 因此不受另外两套「动态 PolicyProvider」影响（它们都优先返回 AddPolicy 注册过的策略）。
/// </summary>
public static class ResourceAuthorizationPolicyNames
{
    /// <summary>多租户隔离：订单必须属于当前用户所在租户。</summary>
    public const string SameTenant = "Resource.SameTenant";

    /// <summary>数据所有权：订单必须由当前用户本人创建。</summary>
    public const string OrderOwner = "Resource.OrderOwner";
}
