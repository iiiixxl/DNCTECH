namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 订单数据源。真实项目里就是查库（EF Core / Dapper），这里用内存模拟。
/// </summary>
public interface IOrderStore
{
    Order? Find(int id);

    IReadOnlyList<Order> All();
}
