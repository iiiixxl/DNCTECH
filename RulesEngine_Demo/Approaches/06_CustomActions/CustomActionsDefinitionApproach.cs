using RulesEngine.Models;
using RulesEngine_Demo.Approaches.CustomActions.Actions;
using RulesEngine_Demo.Approaches.JsonWorkflow;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.CustomActions;

/// <summary>
/// 定义方式⑥：JSON Workflow + 自定义 Action（继承 ActionBase，注册到 ReSettings.CustomActions）。
/// </summary>
public sealed class CustomActionsDefinitionApproach : IDefinitionApproach
{
    public string Key => "6";
    public string Title => "自定义 Action（CustomActions）";
    public string Folder => "06_CustomActions";
    public string Summary => "规则命中后执行自研 Action：写折扣结果、推送审计队列，而不仅是 OutputExpression";

    public async Task RunAsync()
    {
        ConsoleUi.Title($"[{Key}] {Title}");
        Console.WriteLine(Summary);
        ApproachHelpers.PrintDocHint(Folder);

        SendAuditLogAction.Clear();

        var customActions = new Dictionary<string, Func<RulesEngine.Actions.ActionBase>>
        {
            ["ApplyDiscount"] = () => new ApplyDiscountAction(),
            ["SendAuditLog"] = () => new SendAuditLogAction()
        };

        var settings = RuleEngineHost.CreateDefaultSettings(customActions);
        var host = new RuleEngineHost(settings);

        var rulesDir = Path.Combine(RuleEngineHost.ApproachPath(Folder), "Rules");
        host.UseWorkflows(JsonWorkflowLoader.LoadDirectory(rulesDir), settings);
        Console.WriteLine($"规则目录: {rulesDir}");
        Console.WriteLine($"已加载: {string.Join(", ", host.Workflows.Select(w => w.WorkflowName))}");
        ConsoleUi.Info("已注册 CustomActions: ApplyDiscount, SendAuditLog");

        var cases = new (string Label, CustomerInput Customer, OrderInput Order)[]
        {
            ("金牌会员 + 数码满减",
                new CustomerInput { CustomerId = "C-CA1", Name = "李四", Level = MemberLevel.Gold },
                new OrderInput
                {
                    OrderId = "O-CA1", Amount = 2599, ItemCount = 2, Category = "Electronics",
                    IsFirstOrder = false, UsedCoupon = false
                }),
            ("VIP 大单（触发折扣 + 双审计）",
                new CustomerInput { CustomerId = "C-CA2", Name = "王五", Level = MemberLevel.Vip },
                new OrderInput
                {
                    OrderId = "O-CA2", Amount = 6800, ItemCount = 5, Category = "Fashion",
                    IsFirstOrder = false
                }),
            ("白银会员小单（仅会员折）",
                new CustomerInput { CustomerId = "C-CA3", Name = "赵六", Level = MemberLevel.Silver },
                new OrderInput
                {
                    OrderId = "O-CA3", Amount = 880, ItemCount = 1, Category = "Fashion"
                }),
        };

        foreach (var (label, customer, order) in cases)
        {
            ConsoleUi.Section(label);
            Console.WriteLine($"会员={customer.Name}/{customer.Level}  原价={order.Amount:C}  品类={order.Category}");

            var results = await host.ExecuteAsync(
                "OrderDiscountCustom",
                new RuleParameter("customer", customer),
                new RuleParameter("order", order));

            RuleResultFormatter.Print(results);
            var events = RuleResultFormatter.CollectSuccessEvents(results);
            Console.WriteLine($"命中: {(events.Count == 0 ? "无" : string.Join(", ", events))}");

            foreach (var output in RuleResultFormatter.AllSuccessOutputs(results))
            {
                if (output is string s && (s.StartsWith("discount:", StringComparison.Ordinal)
                                           || s.StartsWith("subtract:", StringComparison.Ordinal)))
                    ConsoleUi.Success($"Action 输出: {s}");
            }
        }

        ConsoleUi.Section("AuditSink（SendAuditLog 写入）");
        if (SendAuditLogAction.AuditSink.IsEmpty)
            ConsoleUi.Warn("队列为空（本批用例未命中审计规则）");
        else
            foreach (var line in SendAuditLogAction.AuditSink)
                Console.WriteLine("  · " + line);

        ConsoleUi.Section("落地提示");
        Console.WriteLine("· CustomActions 字典的 Key 必须与 JSON 里 Actions.OnSuccess.Name 完全一致。");
        Console.WriteLine("· Action 工厂建议无状态或短生命周期；共享状态（如 AuditSink）仅适合 Demo。");
        Console.WriteLine("· 复杂副作用（发邮件/扣库存）放在 Action 或应用层编排，勿塞进 Expression。");
    }
}
