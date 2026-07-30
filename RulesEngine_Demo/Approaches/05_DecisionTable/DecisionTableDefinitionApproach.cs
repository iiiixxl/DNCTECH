using RulesEngine.Models;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.DecisionTable;

/// <summary>定义方式⑤：决策表 CSV → 编译为 Workflow。</summary>
public sealed class DecisionTableDefinitionApproach : IDefinitionApproach
{
    public string Key => "5";
    public string Title => "决策表 CSV 定义";
    public string Folder => "05_DecisionTable";
    public string Summary => "用表格维护条件列与输出列，运行时编译成 Expression / OutputExpression";

    public async Task RunAsync()
    {
        var tablesDir = Path.Combine(RuleEngineHost.ApproachPath(Folder), "Tables");
        var memberCsv = Path.Combine(tablesDir, "member-discount.csv");
        var approvalCsv = Path.Combine(tablesDir, "approval-matrix.csv");

        var orderWf = DecisionTableCompiler.CompileMemberDiscount(memberCsv);
        var approvalWf = DecisionTableCompiler.CompileApprovalMatrix(approvalCsv);

        var host = new RuleEngineHost();
        host.UseWorkflows([orderWf, approvalWf]);

        ConsoleUi.Title($"[{Key}] {Title}");
        Console.WriteLine(Summary);
        Console.WriteLine($"表目录: {tablesDir}");
        Console.WriteLine($"已加载: {string.Join(", ", host.Workflows.Select(w => w.WorkflowName))}");
        ApproachHelpers.PrintDocHint(Folder);

        await RunOrderCasesAsync(host);
        await RunApprovalCasesAsync(host);
    }

    private static async Task RunOrderCasesAsync(RuleEngineHost host)
    {
        ConsoleUi.Section("会员折扣决策表");

        var cases = new (string Label, CustomerInput Customer, OrderInput Order)[]
        {
            ("VIP",
                new CustomerInput { CustomerId = "C1", Name = "王五", Level = MemberLevel.Vip },
                new OrderInput { OrderId = "DT-1", Amount = 1000, Category = "Fashion" }),
            ("金牌",
                new CustomerInput { CustomerId = "C2", Name = "李四", Level = MemberLevel.Gold },
                new OrderInput { OrderId = "DT-2", Amount = 800, Category = "Electronics" }),
            ("普通",
                new CustomerInput { CustomerId = "C3", Name = "张三", Level = MemberLevel.Normal },
                new OrderInput { OrderId = "DT-3", Amount = 200, Category = "Fashion" }),
        };

        foreach (var (label, customer, order) in cases)
        {
            ConsoleUi.Section(label);
            var results = await host.ExecuteAsync(
                "OrderDiscount",
                new RuleParameter("customer", customer),
                new RuleParameter("order", order));
            RuleResultFormatter.Print(results.Where(r => r.IsSuccess), includeFailed: false);
            var payable = ApproachHelpers.CalculatePayable(order.Amount, results);
            ConsoleUi.Success($"应付: {payable:C}（原价 {order.Amount:C}）");
        }
    }

    private static async Task RunApprovalCasesAsync(RuleEngineHost host)
    {
        ConsoleUi.Section("审批矩阵决策表");

        var cases = new (string Label, ApprovalInput Input)[]
        {
            ("小额",
                new ApprovalInput { RequestId = "DT-A1", ExpenseType = "Travel", Amount = 2800, VendorRisk = "Low" }),
            ("中额",
                new ApprovalInput { RequestId = "DT-A2", ExpenseType = "Purchase", Amount = 35000, VendorRisk = "Low", HasContract = true }),
            ("CapEx",
                new ApprovalInput { RequestId = "DT-A3", ExpenseType = "CapEx", Amount = 800000, VendorRisk = "Low", HasContract = true }),
            ("高风险供应商",
                new ApprovalInput { RequestId = "DT-A4", ExpenseType = "Purchase", Amount = 45000, VendorRisk = "High" }),
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
