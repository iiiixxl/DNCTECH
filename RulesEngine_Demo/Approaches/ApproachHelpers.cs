using RulesEngine.Models;
using RulesEngine_Demo.Infrastructure;

namespace RulesEngine_Demo.Approaches;

/// <summary>各定义方式 Demo 共用的小工具（结算、打印、读 Properties.Priority）。</summary>
public static class ApproachHelpers
{
    public static void PrintDocHint(string folder)
    {
        var md = Path.Combine(RuleEngineHost.ApproachPath(folder), "README.md");
        ConsoleUi.Info($"详细文档: Approaches/{folder}/README.md");
        if (File.Exists(md))
            ConsoleUi.Info($"磁盘路径: {md}");
    }

    public static int ReadPriority(Rule rule)
    {
        if (rule.Properties != null
            && rule.Properties.TryGetValue("Priority", out var raw)
            && int.TryParse(raw?.ToString(), out var p))
            return p;
        return 0;
    }

    public static decimal CalculatePayable(decimal original, List<RuleResultTree> results)
    {
        decimal factor = 1m;
        decimal subtract = 0m;

        foreach (var output in RuleResultFormatter.AllSuccessOutputs(results))
        {
            switch (output)
            {
                case decimal d when d is > 0 and < 1:
                    factor = Math.Min(factor, d);
                    break;
                case double dbl when dbl is > 0 and < 1:
                    factor = Math.Min(factor, (decimal)dbl);
                    break;
                case decimal money when money < 0:
                    subtract += money;
                    break;
                case double money when money < 0:
                    subtract += (decimal)money;
                    break;
                case int i when i < 0:
                    subtract += i;
                    break;
                case long l when l < 0:
                    subtract += l;
                    break;
                case string s when decimal.TryParse(s, out var parsed):
                    if (parsed is > 0 and < 1) factor = Math.Min(factor, parsed);
                    else if (parsed < 0) subtract += parsed;
                    break;
            }
        }

        return Math.Max(0.01m, Math.Round(original * factor + subtract, 2));
    }
}
