using RulesEngine.Models;
using RulesEngine_Demo.Approaches;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.JsonWorkflow;

/// <summary>JSON 热更新：改磁盘文件 → 重新反序列化 → UseWorkflows。</summary>
public sealed class HotReloadDemo : IJsonBusinessDemo
{
    public string Key => "6";
    public string Title => "规则热更新";
    public string Description => "修改 order-discount.json 后 Reload，观察促销结果变化";

    public async Task RunAsync(RuleEngineHost host, string rulesDirectory)
    {
        ConsoleUi.Title(Title);

        var customer = new CustomerInput
        {
            CustomerId = "C-HOT", Name = "热更新体验官", Level = MemberLevel.Silver, Points = 500
        };
        var order = new OrderInput
        {
            OrderId = "O-HOT", Amount = 1200, ItemCount = 3, Category = "Fashion", IsFirstOrder = false
        };

        ConsoleUi.Section("第一次执行（当前磁盘规则）");
        await RunOnce(host, customer, order);

        var target = Path.Combine(rulesDirectory, "order-discount.json");
        ConsoleUi.Section("操作说明");
        Console.WriteLine($"1) 用编辑器打开: {target}");
        Console.WriteLine("2) 找到规则 SilverMemberDiscount，把 OutputExpression 里的 0.95 改成 0.90");
        Console.WriteLine("3) 保存文件后回到这里按 Enter");
        Console.Write("完成后按 Enter 继续（或直接 Enter 跳过改文件，仅演示 Reload API）...");
        Console.ReadLine();

        host.UseWorkflows(JsonWorkflowLoader.LoadDirectory(rulesDirectory));
        ConsoleUi.Info($"已从 {rulesDirectory} 重新加载，共 {host.Workflows.Count} 个 Workflow。");

        ConsoleUi.Section("第二次执行（Reload 之后）");
        await RunOnce(host, customer, order);

        ConsoleUi.Section("落地提示");
        Console.WriteLine("· 生产：配置中心推送 → 校验 JSON Schema → 原子替换 → 重建 RulesEngine 实例。");
        Console.WriteLine("· 注意：表达式编译有成本，热更新宜防抖；对热点 Workflow 可做版本号灰度。");
        Console.WriteLine("· 务必保留规则版本与执行快照，否则客诉「当时为什么这个价」无法追溯。");
    }

    private static async Task RunOnce(RuleEngineHost host, CustomerInput customer, OrderInput order)
    {
        var results = await host.ExecuteAsync(
            "OrderDiscount",
            new RuleParameter("customer", customer),
            new RuleParameter("order", order));

        RuleResultFormatter.Print(results.Where(r => r.IsSuccess), includeFailed: false);
        var events = RuleResultFormatter.CollectSuccessEvents(results);
        Console.WriteLine($"命中: {(events.Count == 0 ? "无" : string.Join(", ", events))}");
        foreach (var r in results.Where(x => x.IsSuccess && x.Rule.RuleName == "SilverMemberDiscount"))
            ConsoleUi.Success($"SilverMemberDiscount Output = {r.ActionResult?.Output}");
    }
}
