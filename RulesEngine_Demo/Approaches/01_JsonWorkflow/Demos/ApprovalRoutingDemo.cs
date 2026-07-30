using RulesEngine.Models;
using RulesEngine_Demo.Approaches;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.JsonWorkflow;

/// <summary>
/// 场景2：费用/采购审批路由。
/// 业务痛点：金额阈值、部门、供应商风险交叉，硬编码 if-else 极易腐化；审批流常要按制度改。
/// </summary>
public sealed class ApprovalRoutingDemo : IJsonBusinessDemo
{
    public string Key => "2";
    public string Title => "费用审批路由";
    public string Description => "按金额/类型/供应商风险决定审批链，输出下一节点与 SLA";

    public async Task RunAsync(RuleEngineHost host, string rulesDir)
    {
        ConsoleUi.Title(Title);
        ConsoleUi.Info("Workflow: ApprovalRouting");
        ConsoleUi.Info("要点: Properties.Priority + 互斥表达式；命中后取 Output 作为 BPM 节点链。");

        var cases = new (string Label, ApprovalInput Input)[]
        {
            ("销售差旅 2800（经理即可）",
                new ApprovalInput
                {
                    RequestId = "AP-01", ApplicantDept = "Sales", ExpenseType = "Travel",
                    Amount = 2800, VendorRisk = "Low"
                }),

            ("研发采购 45000 + 高风险供应商",
                new ApprovalInput
                {
                    RequestId = "AP-02", ApplicantDept = "R&D", ExpenseType = "Purchase",
                    Amount = 45000, VendorRisk = "High", HasContract = false
                }),

            ("市场活动 12 万紧急投放",
                new ApprovalInput
                {
                    RequestId = "AP-03", ApplicantDept = "Admin", ExpenseType = "Marketing",
                    Amount = 120000, IsUrgent = true, VendorRisk = "Medium", HasContract = true
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
                $"单号={input.RequestId}  部门={input.ApplicantDept}  类型={input.ExpenseType}  " +
                $"金额={input.Amount:C}  风险={input.VendorRisk}  紧急={input.IsUrgent}");

            var results = await host.ExecuteAsync(
                "ApprovalRouting",
                new RuleParameter("req", input));

            // RulesEngine 6 已无内置 Priority；业务元数据放 Properties.Priority，路由取最高者
            var hit = results
                .Where(r => r.IsSuccess && r.ActionResult?.Output is not null)
                .OrderByDescending(r => ApproachHelpers.ReadPriority(r.Rule))
                .ToList();

            RuleResultFormatter.Print(results.Where(r => r.IsSuccess), includeFailed: false);

            var route = hit.FirstOrDefault()?.ActionResult?.Output?.ToString() ?? "AUTO_APPROVE";
            var events = RuleResultFormatter.CollectSuccessEvents(results);

            ConsoleUi.Success($"审批路由: {route}");
            if (events.Count > 0)
                Console.WriteLine($"附加标记: {string.Join(", ", events)}");
        }

        ConsoleUi.Section("落地提示");
        Console.WriteLine("· Output 建议约定为稳定枚举/节点码（如 FINANCE_VP → BPM 网关），避免直接写人名。");
        Console.WriteLine("· 制度变更只改 JSON；应用侧用字典把节点码映射到实际审批人/角色。");
        Console.WriteLine("· 高风险供应商等「强制加签」适合用独立规则 + SuccessEvent，而不是塞进主路由表达式。");
        Console.WriteLine("· v6 无 Rule.Priority，可用 Properties[\"Priority\"] 或把金额区间写成互斥表达式。");
    }
}

