using RulesEngine_Demo.Infrastructure;

namespace RulesEngine_Demo.Approaches.JsonWorkflow;

/// <summary>定义方式①：JSON Workflow 文件（RulesEngine 官方最常见用法）。</summary>
public sealed class JsonDefinitionApproach : IDefinitionApproach
{
    public string Key => "1";
    public string Title => "JSON Workflow 文件定义";
    public string Folder => "01_JsonWorkflow";
    public string Summary => "把 Workflow/Rule 序列化为 .json，运行时反序列化后交给 RulesEngine";

    public async Task RunAsync()
    {
        var rulesDir = Path.Combine(RuleEngineHost.ApproachPath(Folder), "Rules");
        var host = new RuleEngineHost();
        host.UseWorkflows(JsonWorkflowLoader.LoadDirectory(rulesDir));

        ConsoleUi.Title($"[{Key}] {Title}");
        Console.WriteLine(Summary);
        Console.WriteLine($"规则目录: {rulesDir}");
        Console.WriteLine($"已加载: {string.Join(", ", host.Workflows.Select(w => w.WorkflowName))}");
        ApproachHelpers.PrintDocHint(Folder);

        IJsonBusinessDemo[] demos =
        [
            new OrderDiscountDemo(),
            new ApprovalRoutingDemo(),
            new RiskControlDemo(),
            new ContractTermsDemo(),
            new ShippingFeeDemo(),
            new HotReloadDemo()
        ];

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("JSON 业务场景:");
            foreach (var d in demos)
                Console.WriteLine($"  [{d.Key}] {d.Title} — {d.Description}");
            Console.WriteLine("  [A] 跑完全部（跳过热更新）");
            Console.WriteLine("  [B] 返回上级（定义方式菜单）");
            Console.Write("> ");

            var input = (Console.ReadLine() ?? "").Trim();
            if (input.Equals("B", StringComparison.OrdinalIgnoreCase) || input.Equals("Q", StringComparison.OrdinalIgnoreCase))
                return;

            if (input.Equals("A", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var d in demos.Where(x => x.Key != "6"))
                    await d.RunAsync(host, rulesDir);
                ConsoleUi.Pause();
                continue;
            }

            var demo = demos.FirstOrDefault(d => d.Key == input);
            if (demo is null)
            {
                ConsoleUi.Warn("无效选项。");
                continue;
            }

            await demo.RunAsync(host, rulesDir);
            // 热更新可能改了 host 内引擎，保持即可
            ConsoleUi.Pause();
        }
    }

    public async Task RunAllNonInteractiveAsync()
    {
        var rulesDir = Path.Combine(RuleEngineHost.ApproachPath(Folder), "Rules");
        var host = new RuleEngineHost();
        host.UseWorkflows(JsonWorkflowLoader.LoadDirectory(rulesDir));
        ConsoleUi.Title($"[{Key}] {Title}");
        foreach (var d in new IJsonBusinessDemo[]
                 {
                     new OrderDiscountDemo(), new ApprovalRoutingDemo(), new RiskControlDemo(),
                     new ContractTermsDemo(), new ShippingFeeDemo()
                 })
            await d.RunAsync(host, rulesDir);
    }
}
