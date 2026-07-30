using System.Globalization;
using System.Text;
using RulesEngine.Models;

namespace RulesEngine_Demo.Approaches.DecisionTable;

/// <summary>将决策表 CSV 编译为 RulesEngine Workflow。</summary>
public static class DecisionTableCompiler
{
    public static Workflow CompileMemberDiscount(string csvPath)
    {
        var rules = new List<Rule>();
        foreach (var row in ReadCsv(csvPath))
        {
            var ruleName = Req(row, "RuleName");
            var level = Req(row, "MemberLevel");
            var minAmount = Req(row, "MinAmount");
            var factor = Req(row, "DiscountFactor");
            var successEvent = Req(row, "SuccessEvent");
            var priority = Get(row, "Priority") ?? "0";

            // customer.Level == 3 && order.Amount >= 0
            var expression = $"customer.Level == {level} && order.Amount >= {minAmount}";

            rules.Add(new Rule
            {
                RuleName = ruleName,
                Enabled = true,
                Expression = expression,
                SuccessEvent = successEvent,
                Properties = new Dictionary<string, object> { ["Priority"] = priority },
                Actions = Output(factor)
            });
        }

        return new Workflow
        {
            WorkflowName = "OrderDiscount",
            Rules = rules
        };
    }

    public static Workflow CompileApprovalMatrix(string csvPath)
    {
        var rules = new List<Rule>();
        foreach (var row in ReadCsv(csvPath))
        {
            var ruleName = Req(row, "RuleName");
            var minAmount = Get(row, "MinAmount");
            var maxAmount = Get(row, "MaxAmount");
            var expenseType = Get(row, "ExpenseType");
            var vendorRisk = Get(row, "VendorRisk");
            var route = Req(row, "Route");
            var successEvent = Req(row, "SuccessEvent");
            var priority = Get(row, "Priority") ?? "0";

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(minAmount))
                parts.Add($"req.Amount >= {minAmount}");
            if (!string.IsNullOrWhiteSpace(maxAmount))
                parts.Add($"req.Amount < {maxAmount}");
            if (!string.IsNullOrWhiteSpace(expenseType))
                parts.Add($"req.ExpenseType == \"{Escape(expenseType)}\"");
            if (!string.IsNullOrWhiteSpace(vendorRisk))
                parts.Add($"req.VendorRisk == \"{Escape(vendorRisk)}\"");

            // CapEx 行：金额下限可空；VeryLargeAny 无 ExpenseType，靠金额
            if (parts.Count == 0)
                parts.Add("true");

            var expression = string.Join(" && ", parts);
            // OutputExpression 需要字符串字面量
            var outputExpr = $"\"{Escape(route)}\"";

            rules.Add(new Rule
            {
                RuleName = ruleName,
                Enabled = true,
                Expression = expression,
                SuccessEvent = successEvent,
                Properties = new Dictionary<string, object> { ["Priority"] = priority },
                Actions = Output(outputExpr)
            });
        }

        return new Workflow
        {
            WorkflowName = "ApprovalRouting",
            Rules = rules
        };
    }

    private static RuleActions Output(string expression) => new()
    {
        OnSuccess = new ActionInfo
        {
            Name = "OutputExpression",
            Context = new Dictionary<string, object> { ["Expression"] = expression }
        }
    };

    private static string Escape(string s) => s.Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string Req(Dictionary<string, string> row, string key) =>
        Get(row, key) ?? throw new InvalidOperationException($"CSV 缺少列/值: {key}");

    private static string? Get(Dictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

    private static IEnumerable<Dictionary<string, string>> ReadCsv(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))
            .ToList();
        if (lines.Count < 2)
            yield break;

        var headers = SplitCsvLine(lines[0])
            .Select(h => h.Trim().TrimStart('\uFEFF'))
            .ToArray();
        for (var i = 1; i < lines.Count; i++)
        {
            var cols = SplitCsvLine(lines[i]);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Length; c++)
            {
                if (string.IsNullOrEmpty(headers[c])) continue;
                dict[headers[c]] = c < cols.Length ? cols[c].Trim() : "";
            }
            yield return dict;
        }
    }

    /// <summary>简单 CSV 拆分（支持双引号包裹字段）。</summary>
    private static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }

        result.Add(sb.ToString());
        return result.ToArray();
    }
}
