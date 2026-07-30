using RulesEngine.Models;
using RulesEngine_Demo.Approaches;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.JsonWorkflow;

/// <summary>
/// 场景1：电商订单促销计价。
/// 业务痛点：运营天天改满减/会员折/品类活动，不能发版；规则要可叠加、可审计。
/// </summary>
public sealed class OrderDiscountDemo : IJsonBusinessDemo
{
    public string Key => "1";
    public string Title => "电商订单促销计价";
    public string Description => "会员折扣 + 满减 + 品类活动 + 首单礼，输出应付金额与命中标签";

    public async Task RunAsync(RuleEngineHost host, string rulesDir)
    {
        ConsoleUi.Title(Title);
        ConsoleUi.Info("Workflow: OrderDiscount");
        ConsoleUi.Info("要点: SuccessEvent 收集促销标签；OutputExpression 输出折扣系数/减免额；嵌套 And 规则。");

        var cases = new (string Label, CustomerInput Customer, OrderInput Order)[]
        {
            ("普通用户小单（几乎无优惠）",
                new CustomerInput { CustomerId = "C001", Name = "张三", Level = MemberLevel.Normal, Points = 20 },
                new OrderInput { OrderId = "O-1001", Amount = 89, ItemCount = 1, Category = "Fashion", IsFirstOrder = false }),

            ("金牌会员 + 数码满减 + 首单",
                new CustomerInput { CustomerId = "C002", Name = "李四", Level = MemberLevel.Gold, Points = 3200, SpendThisYear = 18000 },
                new OrderInput { OrderId = "O-1002", Amount = 2599, ItemCount = 2, Category = "Electronics", IsFirstOrder = true }),

            ("VIP 大单 + 优惠券互斥演示",
                new CustomerInput { CustomerId = "C003", Name = "王五", Level = MemberLevel.Vip, Points = 9000, SpendThisYear = 52000 },
                new OrderInput
                {
                    OrderId = "O-1003", Amount = 6800, ItemCount = 5, Category = "Electronics",
                    IsFirstOrder = false, UsedCoupon = true, CouponCode = "SAVE100"
                }),
        };

        foreach (var (label, customer, order) in cases)
        {
            ConsoleUi.Section(label);
            Console.WriteLine($"会员={customer.Name}/{customer.Level}  原价={order.Amount:C}  品类={order.Category}  首单={order.IsFirstOrder}  用券={order.UsedCoupon}");

            var results = await host.ExecuteAsync(
                "OrderDiscount",
                new RuleParameter("customer", customer),
                new RuleParameter("order", order));

            RuleResultFormatter.Print(results);

            var events = RuleResultFormatter.CollectSuccessEvents(results);
            var payable = ApproachHelpers.CalculatePayable(order.Amount, results);

            Console.WriteLine($"命中促销: {(events.Count == 0 ? "无" : string.Join(", ", events))}");
            ConsoleUi.Success($"应付金额: {payable:C}  （原价 {order.Amount:C}，优惠 {order.Amount - payable:C}）");
        }

        ConsoleUi.Section("落地提示");
        Console.WriteLine("· 规则 JSON 由运营/配置中心维护，应用只负责传输入、解释输出。");
        Console.WriteLine("· SuccessEvent 可写入订单扩展字段，便于对账与客服解释「为什么便宜了」。");
        Console.WriteLine("· 折扣叠加策略（取最优 / 累乘 / 互斥）应在业务代码中显式实现，不要全塞进表达式。");
    }
}

