using System.Text.Json;
using RulesEngine.Models;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.ConfigStore;

/// <summary>
/// 定义方式⑦：规则存配置库（本 Demo 用内存字典模拟），Upsert 后重建引擎实现「库内热更新」。
/// </summary>
public sealed class ConfigStoreDefinitionApproach : IDefinitionApproach
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public string Key => "7";
    public string Title => "配置库 / 规则商店热更新";
    public string Folder => "07_ConfigStore";
    public string Summary => "用 InMemoryRuleStore 模拟 DB/配置中心：种子 JSON → 执行 → Upsert 改价 → 重建引擎再执行";

    public async Task RunAsync()
    {
        ConsoleUi.Title($"[{Key}] {Title}");
        Console.WriteLine(Summary);
        ApproachHelpers.PrintDocHint(Folder);

        var store = new InMemoryRuleStore();
        store.Upsert("OrderDiscount", SeedOrderDiscountJson(silverFactor: "0.95"));
        ConsoleUi.Info($"种子写入完成，OrderDiscount 版本 = v{store.GetVersion("OrderDiscount")}");

        var host = new RuleEngineHost();
        LoadFromStore(host, store);

        var customer = new CustomerInput
        {
            CustomerId = "C-DB1", Name = "热更新体验官", Level = MemberLevel.Silver, Points = 500
        };
        var order = new OrderInput
        {
            OrderId = "O-DB1", Amount = 1200, ItemCount = 3, Category = "Fashion", IsFirstOrder = false
        };

        ConsoleUi.Section($"第一次执行（库内 Silver 折扣 = 0.95，版本 v{store.GetVersion("OrderDiscount")}）");
        await RunOnceAsync(host, customer, order);

        // 模拟运营在配置中心把白银折扣从 95 折改成 90 折
        store.Upsert("OrderDiscount", SeedOrderDiscountJson(silverFactor: "0.90"));
        ConsoleUi.Section($"模拟 DB Upsert：Silver 0.95 → 0.90，新版本 v{store.GetVersion("OrderDiscount")}");
        LoadFromStore(host, store);
        ConsoleUi.Success("已从配置库重新反序列化并 UseWorkflows。");

        ConsoleUi.Section("第二次执行（热更新之后）");
        await RunOnceAsync(host, customer, order);

        ConsoleUi.Section("落地提示");
        Console.WriteLine("· 生产：配置中心推送 / DB 变更 → Schema 校验 → 版本号原子切换 → 重建 RulesEngine。");
        Console.WriteLine("· 务必保留「规则版本 + 输入快照」，否则客诉无法复盘当时计价。");
        Console.WriteLine("· 本 Demo 的 InMemoryRuleStore 可替换为 EF/Redis/Nacos，对外仍是 GetAllJson + Upsert。");
    }

    private static void LoadFromStore(RuleEngineHost host, InMemoryRuleStore store)
    {
        var workflows = new List<Workflow>();
        foreach (var json in store.GetAllJson())
        {
            var wf = JsonSerializer.Deserialize<Workflow>(json, JsonOptions)
                     ?? throw new InvalidOperationException("配置库中的 JSON 无法反序列化为 Workflow。");
            workflows.Add(wf);
        }

        host.UseWorkflows(workflows);
    }

    private static async Task RunOnceAsync(RuleEngineHost host, CustomerInput customer, OrderInput order)
    {
        Console.WriteLine($"会员={customer.Name}/{customer.Level}  原价={order.Amount:C}");

        var results = await host.ExecuteAsync(
            "OrderDiscount",
            new RuleParameter("customer", customer),
            new RuleParameter("order", order));

        RuleResultFormatter.Print(results.Where(r => r.IsSuccess), includeFailed: false);
        var events = RuleResultFormatter.CollectSuccessEvents(results);
        Console.WriteLine($"命中: {(events.Count == 0 ? "无" : string.Join(", ", events))}");

        foreach (var r in results.Where(x => x.IsSuccess && x.Rule.RuleName == "SilverMemberDiscount"))
            ConsoleUi.Success($"SilverMemberDiscount Output = {r.ActionResult?.Output}");

        var payable = ApproachHelpers.CalculatePayable(order.Amount, results);
        ConsoleUi.Success($"应付金额: {payable:C}");
    }

    /// <summary>内联种子：仅保留会员折扣子集，便于观察 Silver 系数变化。</summary>
    private static string SeedOrderDiscountJson(string silverFactor) => $$"""
        {
          "WorkflowName": "OrderDiscount",
          "Rules": [
            {
              "RuleName": "VipMemberDiscount",
              "Enabled": true,
              "Priority": 100,
              "Expression": "customer.Level == 3",
              "SuccessEvent": "VipDiscount15Off",
              "Actions": {
                "OnSuccess": {
                  "Name": "OutputExpression",
                  "Context": { "Expression": "0.85" }
                }
              }
            },
            {
              "RuleName": "GoldMemberDiscount",
              "Enabled": true,
              "Priority": 90,
              "Expression": "customer.Level == 2",
              "SuccessEvent": "GoldDiscount10Off",
              "Actions": {
                "OnSuccess": {
                  "Name": "OutputExpression",
                  "Context": { "Expression": "0.90" }
                }
              }
            },
            {
              "RuleName": "SilverMemberDiscount",
              "Enabled": true,
              "Priority": 80,
              "Expression": "customer.Level == 1",
              "SuccessEvent": "SilverDiscount5Off",
              "Actions": {
                "OnSuccess": {
                  "Name": "OutputExpression",
                  "Context": { "Expression": "{{silverFactor}}" }
                }
              }
            }
          ]
        }
        """;
}
