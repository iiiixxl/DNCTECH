using RulesEngine.Models;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.CodeWorkflow;

/// <summary>定义方式②：纯 C# 代码组装 Workflow / Rule 对象树。</summary>
public sealed class CodeDefinitionApproach : IDefinitionApproach
{
    public string Key => "2";
    public string Title => "C# 代码定义 Workflow";
    public string Folder => "02_CodeWorkflow";
    public string Summary => "在代码中 new Workflow / Rule，适合强类型、可重构、随应用发布的规则";

    public async Task RunAsync()
    {
        var host = new RuleEngineHost();
        host.UseWorkflows(CodeWorkflowFactory.CreateAll());

        ConsoleUi.Title($"[{Key}] {Title}");
        Console.WriteLine(Summary);
        Console.WriteLine($"已加载: {string.Join(", ", host.Workflows.Select(w => w.WorkflowName))}");
        ApproachHelpers.PrintDocHint(Folder);

        await RunOrderDiscountCasesAsync(host);
        await RunApprovalCasesAsync(host);
    }

    private static async Task RunOrderDiscountCasesAsync(RuleEngineHost host)
    {
        ConsoleUi.Section("订单促销（代码 Workflow）");

        var cases = new (string Label, CustomerInput Customer, OrderInput Order)[]
        {
            ("金牌会员 + 数码满减 + 首单",
                new CustomerInput { CustomerId = "C002", Name = "李四", Level = MemberLevel.Gold, Points = 3200 },
                new OrderInput { OrderId = "O-1002", Amount = 2599, ItemCount = 2, Category = "Electronics", IsFirstOrder = true }),

            ("VIP 大单 + 优惠券互斥",
                new CustomerInput { CustomerId = "C003", Name = "王五", Level = MemberLevel.Vip, Points = 9000 },
                new OrderInput
                {
                    OrderId = "O-1003", Amount = 6800, ItemCount = 5, Category = "Electronics",
                    UsedCoupon = true, CouponCode = "SAVE100"
                }),

            ("银牌小单",
                new CustomerInput { CustomerId = "C004", Name = "赵六", Level = MemberLevel.Silver },
                new OrderInput { OrderId = "O-1004", Amount = 199, ItemCount = 1, Category = "Fashion" }),
        };

        foreach (var (label, customer, order) in cases)
        {
            ConsoleUi.Section(label);
            Console.WriteLine($"会员={customer.Name}/{customer.Level}  原价={order.Amount:C}  品类={order.Category}");

            var results = await host.ExecuteAsync(
                "OrderDiscount",
                new RuleParameter("customer", customer),
                new RuleParameter("order", order));

            RuleResultFormatter.Print(results);
            var events = RuleResultFormatter.CollectSuccessEvents(results);
            var payable = ApproachHelpers.CalculatePayable(order.Amount, results);

            Console.WriteLine($"命中促销: {(events.Count == 0 ? "无" : string.Join(", ", events))}");
            ConsoleUi.Success($"应付金额: {payable:C}  （原价 {order.Amount:C}）");
        }
    }

    private static async Task RunApprovalCasesAsync(RuleEngineHost host)
    {
        ConsoleUi.Section("审批路由（代码 Workflow）");

        var cases = new (string Label, ApprovalInput Input)[]
        {
            ("销售差旅 2800",
                new ApprovalInput
                {
                    RequestId = "AP-01", ApplicantDept = "Sales", ExpenseType = "Travel",
                    Amount = 2800, VendorRisk = "Low"
                }),
            ("资本开支 CapEx 80 万",
                new ApprovalInput
                {
                    RequestId = "AP-04", ApplicantDept = "Finance", ExpenseType = "CapEx",
                    Amount = 800000, HasContract = true, VendorRisk = "Low"
                }),
        };

        foreach (var (label, input) in cases)
        {
            ConsoleUi.Section(label);
            Console.WriteLine(
                $"单号={input.RequestId}  类型={input.ExpenseType}  金额={input.Amount:C}  风险={input.VendorRisk}");

            var results = await host.ExecuteAsync("ApprovalRouting", new RuleParameter("req", input));

            var hit = results
                .Where(r => r.IsSuccess && r.ActionResult?.Output is not null)
                .OrderByDescending(r => ApproachHelpers.ReadPriority(r.Rule))
                .ToList();

            RuleResultFormatter.Print(results.Where(r => r.IsSuccess), includeFailed: false);
            var route = hit.FirstOrDefault()?.ActionResult?.Output?.ToString() ?? "AUTO_APPROVE";
            ConsoleUi.Success($"审批路由: {route}");
        }
    }
}
