using RulesEngine.Models;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.YamlWorkflow;

/// <summary>定义方式③：YAML Workflow 文件（YamlDotNet → Workflow）。</summary>
public sealed class YamlDefinitionApproach : IDefinitionApproach
{
    public string Key => "3";
    public string Title => "YAML Workflow 文件定义";
    public string Folder => "03_YamlWorkflow";
    public string Summary => "用 YAML 描述 Workflow，经 YamlDotNet 映射为 RulesEngine 模型";

    public async Task RunAsync()
    {
        var rulesDir = Path.Combine(RuleEngineHost.ApproachPath(Folder), "Rules");
        var host = new RuleEngineHost();
        host.UseWorkflows(YamlWorkflowLoader.LoadDirectory(rulesDir));

        ConsoleUi.Title($"[{Key}] {Title}");
        Console.WriteLine(Summary);
        Console.WriteLine($"规则目录: {rulesDir}");
        Console.WriteLine($"已加载: {string.Join(", ", host.Workflows.Select(w => w.WorkflowName))}");
        ApproachHelpers.PrintDocHint(Folder);

        await RunOrderDiscountCasesAsync(host);
        await RunApprovalCasesAsync(host);
    }

    private static async Task RunOrderDiscountCasesAsync(RuleEngineHost host)
    {
        ConsoleUi.Section("订单促销（YAML）");

        var cases = new (string Label, CustomerInput Customer, OrderInput Order)[]
        {
            ("VIP 会员",
                new CustomerInput { CustomerId = "C003", Name = "王五", Level = MemberLevel.Vip },
                new OrderInput { OrderId = "O-Y1", Amount = 1200, Category = "Fashion" }),
            ("金牌 + 首单",
                new CustomerInput { CustomerId = "C002", Name = "李四", Level = MemberLevel.Gold },
                new OrderInput { OrderId = "O-Y2", Amount = 399, Category = "Fresh", IsFirstOrder = true }),
            ("银牌",
                new CustomerInput { CustomerId = "C004", Name = "赵六", Level = MemberLevel.Silver },
                new OrderInput { OrderId = "O-Y3", Amount = 200, Category = "Fashion" }),
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

    private static async Task RunApprovalCasesAsync(RuleEngineHost host)
    {
        ConsoleUi.Section("审批路由（YAML）");

        var cases = new (string Label, ApprovalInput Input)[]
        {
            ("小额差旅",
                new ApprovalInput
                {
                    RequestId = "AP-Y1", ExpenseType = "Travel", Amount = 2800, VendorRisk = "Low"
                }),
            ("CapEx 大额",
                new ApprovalInput
                {
                    RequestId = "AP-Y2", ExpenseType = "CapEx", Amount = 800000, VendorRisk = "Low", HasContract = true
                }),
        };

        foreach (var (label, input) in cases)
        {
            ConsoleUi.Section(label);
            var results = await host.ExecuteAsync("ApprovalRouting", new RuleParameter("req", input));
            var hit = results
                .Where(r => r.IsSuccess && r.ActionResult?.Output is not null)
                .OrderByDescending(r => ApproachHelpers.ReadPriority(r.Rule))
                .ToList();
            RuleResultFormatter.Print(results.Where(r => r.IsSuccess), includeFailed: false);
            ConsoleUi.Success($"审批路由: {hit.FirstOrDefault()?.ActionResult?.Output ?? "AUTO_APPROVE"}");
        }
    }
}
