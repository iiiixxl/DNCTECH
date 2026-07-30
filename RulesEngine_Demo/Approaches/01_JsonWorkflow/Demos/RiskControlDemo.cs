using RulesEngine.Models;
using RulesEngine_Demo.Approaches;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.JsonWorkflow;

/// <summary>
/// 场景3：支付风控决策。
/// 业务痛点：风控策略频繁调参；要同时输出处置动作（放行/复核/拦截）与可读原因码。
/// </summary>
public sealed class RiskControlDemo : IJsonBusinessDemo
{
    public string Key => "3";
    public string Title => "支付风控决策";
    public string Description => "多信号打分/命中 → Pass / Review / Block，并输出原因码";

    public async Task RunAsync(RuleEngineHost host, string rulesDir)
    {
        ConsoleUi.Title(Title);
        ConsoleUi.Info("Workflow: RiskControl");
        ConsoleUi.Info("要点: 多规则并行命中；业务侧按严重级别归并最终决策（Block > Review > Pass）。");

        var cases = new (string Label, RiskInput Input)[]
        {
            ("老客可信设备正常支付",
                new RiskInput
                {
                    PaymentId = "P-01", Amount = 199, AvgOrderAmount30d = 180,
                    AccountAgeDays = 400, DeviceTrusted = true, IpCountryMatchesBilling = true,
                    FailedAttempts1h = 0, IsNightTime = false
                }),

            ("新账户夜间大额 + IP 国别不符",
                new RiskInput
                {
                    PaymentId = "P-02", Amount = 9800, AvgOrderAmount30d = 120,
                    AccountAgeDays = 2, DeviceTrusted = false, IpCountryMatchesBilling = false,
                    FailedAttempts1h = 1, IsNightTime = true, Channel = "Web"
                }),

            ("短时多次失败后大额重试",
                new RiskInput
                {
                    PaymentId = "P-03", Amount = 3200, AvgOrderAmount30d = 600,
                    AccountAgeDays = 90, DeviceTrusted = true, IpCountryMatchesBilling = true,
                    FailedAttempts1h = 5, IsNightTime = false, Channel = "App"
                }),
        };

        foreach (var (label, input) in cases)
        {
            ConsoleUi.Section(label);
            Console.WriteLine(
                $"支付={input.PaymentId}  金额={input.Amount:C}  账号龄={input.AccountAgeDays}天  " +
                $"可信设备={input.DeviceTrusted}  国别一致={input.IpCountryMatchesBilling}  " +
                $"1h失败={input.FailedAttempts1h}  夜间={input.IsNightTime}");

            var results = await host.ExecuteAsync(
                "RiskControl",
                new RuleParameter("risk", input));

            RuleResultFormatter.Print(results);

            var decision = ResolveDecision(results);
            var reasons = RuleResultFormatter.CollectSuccessEvents(results);

            switch (decision)
            {
                case "Block":
                    ConsoleUi.Error($"最终决策: BLOCK  原因: {string.Join(" | ", reasons)}");
                    break;
                case "Review":
                    ConsoleUi.Warn($"最终决策: REVIEW  原因: {string.Join(" | ", reasons)}");
                    break;
                default:
                    ConsoleUi.Success("最终决策: PASS");
                    break;
            }
        }

        ConsoleUi.Section("落地提示");
        Console.WriteLine("· 规则输出「信号」，决策矩阵留在代码：便于单测、灰度与人工复核台展示原因。");
        Console.WriteLine("· 阈值（如金额倍数、失败次数）放 JSON，方便风控同学调参而不改代码。");
        Console.WriteLine("· 生产可把命中事件打到审计日志 / 风控特征仓，支撑事后案件分析。");
    }

    private static string ResolveDecision(List<RuleResultTree> results)
    {
        var outputs = RuleResultFormatter.AllSuccessOutputs(results)
            .Select(o => o.ToString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (outputs.Contains("Block")) return "Block";
        if (outputs.Contains("Review")) return "Review";
        return "Pass";
    }
}

