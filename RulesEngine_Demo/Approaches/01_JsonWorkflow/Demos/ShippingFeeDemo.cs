using RulesEngine.Models;
using RulesEngine_Demo.Approaches;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.JsonWorkflow;

/// <summary>
/// 场景5：物流运费计算。
/// 业务痛点：首重续重、偏远加价、会员包邮阈值常改；适合规则 + 少量公式。
/// </summary>
public sealed class ShippingFeeDemo : IJsonBusinessDemo
{
    public string Key => "5";
    public string Title => "物流运费计算";
    public string Description => "包邮判定、续重、偏远加价、时效产品加价";

    public async Task RunAsync(RuleEngineHost host, string rulesDir)
    {
        ConsoleUi.Title(Title);
        ConsoleUi.Info("Workflow: ShippingFee");
        ConsoleUi.Info("要点: 先匹配「免运费」短路；否则累加各加价规则的 Output 金额。");

        var cases = new (string Label, ShippingInput Input)[]
        {
            ("VIP 满包邮门槛",
                new ShippingInput
                {
                    OrderId = "S-01", OrderAmount = 299, WeightKg = 1.2m, DistanceKm = 30,
                    ShippingMethod = "Standard", MemberLevel = MemberLevel.Vip
                }),

            ("普通件跨城续重",
                new ShippingInput
                {
                    OrderId = "S-02", OrderAmount = 68, WeightKg = 4.5m, DistanceKm = 800,
                    ShippingMethod = "Standard", MemberLevel = MemberLevel.Normal
                }),

            ("偏远地区次日达",
                new ShippingInput
                {
                    OrderId = "S-03", OrderAmount = 120, WeightKg = 2.0m, DistanceKm = 2200,
                    ShippingMethod = "Express", IsRemoteArea = true, MemberLevel = MemberLevel.Silver
                }),
        };

        foreach (var (label, input) in cases)
        {
            ConsoleUi.Section(label);
            Console.WriteLine(
                $"订单={input.OrderId}  货款={input.OrderAmount:C}  重量={input.WeightKg}kg  " +
                $"距离={input.DistanceKm}km  方式={input.ShippingMethod}  偏远={input.IsRemoteArea}  会员={input.MemberLevel}");

            var results = await host.ExecuteAsync(
                "ShippingFee",
                new RuleParameter("ship", input));

            RuleResultFormatter.Print(results);

            var fee = CalculateFee(results);
            var events = RuleResultFormatter.CollectSuccessEvents(results);
            if (events.Contains("FreeShipping"))
                ConsoleUi.Success("运费: 0.00（包邮）");
            else
                ConsoleUi.Success($"运费: {fee:C}  明细标签: {(events.Count == 0 ? "-" : string.Join(", ", events))}");
        }

        ConsoleUi.Section("落地提示");
        Console.WriteLine("· 复杂计费（分区首重续重表）可仍用代码/价卡表；规则引擎负责「是否包邮、是否加价、用哪张价卡」。");
        Console.WriteLine("· 本 Demo 把金额直接写在 OutputExpression，便于理解；生产更常见是输出价卡 Code。");
    }

    private static decimal CalculateFee(List<RuleResultTree> results)
    {
        var events = RuleResultFormatter.CollectSuccessEvents(results);
        if (events.Contains("FreeShipping"))
            return 0m;

        decimal fee = 0m;
        foreach (var output in RuleResultFormatter.AllSuccessOutputs(results))
        {
            fee += output switch
            {
                decimal d => d,
                double dbl => (decimal)dbl,
                int i => i,
                long l => l,
                string s when decimal.TryParse(s, out var p) => p,
                _ => 0m
            };
        }

        return Math.Round(fee, 2);
    }
}

