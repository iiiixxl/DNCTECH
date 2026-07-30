using RulesEngine.Models;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.FluentBuilder;

/// <summary>定义方式④：Fluent Builder 拼装订单促销规则。</summary>
public sealed class FluentDefinitionApproach : IDefinitionApproach
{
    public string Key => "4";
    public string Title => "Fluent Builder 定义";
    public string Folder => "04_FluentBuilder";
    public string Summary => "链式 API 构建 Workflow，兼顾可读性与编译期检查";

    public async Task RunAsync()
    {
        var workflow = BuildOrderDiscount();
        var host = new RuleEngineHost();
        host.UseWorkflows([workflow]);

        ConsoleUi.Title($"[{Key}] {Title}");
        Console.WriteLine(Summary);
        Console.WriteLine($"已加载: {workflow.WorkflowName}（{workflow.Rules?.Count()} 条规则）");
        ApproachHelpers.PrintDocHint(Folder);

        var cases = new (string Label, CustomerInput Customer, OrderInput Order)[]
        {
            ("金牌 + 数码满减 + 首单",
                new CustomerInput { CustomerId = "C002", Name = "李四", Level = MemberLevel.Gold },
                new OrderInput
                {
                    OrderId = "O-F1", Amount = 2599, Category = "Electronics", IsFirstOrder = true
                }),
            ("VIP + SAVE100 券",
                new CustomerInput { CustomerId = "C003", Name = "王五", Level = MemberLevel.Vip },
                new OrderInput
                {
                    OrderId = "O-F2", Amount = 6800, Category = "Electronics",
                    UsedCoupon = true, CouponCode = "SAVE100"
                }),
            ("银牌小单",
                new CustomerInput { CustomerId = "C004", Name = "赵六", Level = MemberLevel.Silver },
                new OrderInput { OrderId = "O-F3", Amount = 150, Category = "Fashion" }),
        };

        foreach (var (label, customer, order) in cases)
        {
            ConsoleUi.Section(label);
            Console.WriteLine($"会员={customer.Name}/{customer.Level}  原价={order.Amount:C}");

            var results = await host.ExecuteAsync(
                "OrderDiscount",
                new RuleParameter("customer", customer),
                new RuleParameter("order", order));

            RuleResultFormatter.Print(results);
            var payable = ApproachHelpers.CalculatePayable(order.Amount, results);
            ConsoleUi.Success($"应付金额: {payable:C}");
        }
    }

    public static Workflow BuildOrderDiscount() =>
        WorkflowBuilder.Create("OrderDiscount")
            .Rule("VipMemberDiscount")
                .When("customer.Level == 3")
                .Event("VipDiscount15Off")
                .Output("0.85")
                .WithPriority(100)
                .Add()
            .Rule("GoldMemberDiscount")
                .When("customer.Level == 2")
                .Event("GoldDiscount10Off")
                .Output("0.90")
                .WithPriority(90)
                .Add()
            .Rule("SilverMemberDiscount")
                .When("customer.Level == 1")
                .Event("SilverDiscount5Off")
                .Output("0.95")
                .WithPriority(80)
                .Add()
            .Rule("ElectronicsFullReduce")
                .AndChild("IsElectronics", "order.Category == \"Electronics\"")
                .AndChild("AmountOver2000", "order.Amount >= 2000")
                .AndChild("NotUsingCoupon", "order.UsedCoupon == false")
                .Event("ElectronicsMinus200")
                .Output("-200")
                .WithPriority(70)
                .Add()
            .Rule("FirstOrderGift")
                .When("order.IsFirstOrder == true && order.Amount >= 99")
                .Event("FirstOrderMinus50")
                .Output("-50")
                .WithPriority(60)
                .Add()
            .Rule("CouponSave100")
                .When("order.UsedCoupon == true && order.CouponCode == \"SAVE100\" && order.Amount >= 500")
                .Event("CouponSAVE100")
                .Output("-100")
                .WithPriority(50)
                .Add()
            .Build();
}
