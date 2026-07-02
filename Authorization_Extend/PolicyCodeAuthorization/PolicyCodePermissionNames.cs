namespace Authorization_Extend.PolicyCodeAuthorization;

/// <summary>
/// 极简动态权限编码常量。策略名 = 权限编码，无需 AddPolicy。
/// </summary>
public static class PolicyCodePermissionNames
{
    public const string UserView = "User.View";
    public const string UserDelete = "User.Delete";
    public const string OrderCreate = "Order.Create";

    /// <summary>走「用户直查库」模式的权限编码集合，PolicyProvider 据此路由到对应 Handler。</summary>
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        UserView,
        UserDelete,
        OrderCreate
    };
}
