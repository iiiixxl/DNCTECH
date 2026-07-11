namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 内存订单表。预置几条数据，专门用来演示「跨租户」「非本人」两种越权拦截。
/// </summary>
/// <remarks>
/// 配合 AuthController 的登录身份：
/// - admin  → userId = user-admin，  tenant_id = tenant-a
/// - user   → userId = user-normal， tenant_id = tenant-b
/// </remarks>
public class InMemoryOrderStore : IOrderStore
{
    private readonly List<Order> _orders =
    [
        new Order { Id = 1, TenantId = "tenant-a", OwnerUserId = "user-admin",  Amount = 100m, Description = "A 租户-admin 的订单" },
        new Order { Id = 2, TenantId = "tenant-b", OwnerUserId = "user-normal", Amount = 200m, Description = "B 租户-user 的订单" },
        new Order { Id = 3, TenantId = "tenant-a", OwnerUserId = "user-normal", Amount = 300m, Description = "A 租户里、但归属 user 的订单" }
    ];

    public Order? Find(int id) => _orders.FirstOrDefault(o => o.Id == id);

    public IReadOnlyList<Order> All() => _orders;
}
