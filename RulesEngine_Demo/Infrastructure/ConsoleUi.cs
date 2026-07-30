using RulesEngine.Models;

namespace RulesEngine_Demo.Infrastructure;

public static class ConsoleUi
{
    public static void Title(string text)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(new string('═', 64));
        Console.WriteLine($"  {text}");
        Console.WriteLine(new string('═', 64));
        Console.ResetColor();
    }

    public static void Section(string text)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"── {text} ──");
        Console.ResetColor();
    }

    public static void Info(string text)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void Success(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void Warn(string text)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void Error(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void Pause()
    {
        Console.WriteLine();
        Console.Write("按 Enter 返回菜单...");
        Console.ReadLine();
    }
}

/// <summary>把 RuleResultTree 打成可读的审计日志（贴近生产排查）。</summary>
public static class RuleResultFormatter
{
    public static void Print(IEnumerable<RuleResultTree> results, bool includeFailed = true)
    {
        foreach (var r in results)
            PrintOne(r, indent: 0, includeFailed);
    }

    private static void PrintOne(RuleResultTree r, int indent, bool includeFailed)
    {
        if (!r.IsSuccess && !includeFailed && (r.ChildResults == null || !r.ChildResults.Any()))
            return;

        var pad = new string(' ', indent * 2);
        var mark = r.IsSuccess ? "✓" : "✗";
        var color = r.IsSuccess ? ConsoleColor.Green : ConsoleColor.DarkGray;

        Console.ForegroundColor = color;
        Console.Write($"{pad}{mark} [{r.Rule.RuleName}]");
        Console.ResetColor();

        if (!string.IsNullOrWhiteSpace(r.Rule.SuccessEvent) && r.IsSuccess)
            Console.Write($"  event={r.Rule.SuccessEvent}");

        if (r.ActionResult?.Output is not null)
            Console.Write($"  → output={FormatOutput(r.ActionResult.Output)}");

        // ExceptionMessage 在「表达式抛错」时才有价值；单纯未命中不要刷 ErrorMessage
        if (!r.IsSuccess
            && !string.IsNullOrWhiteSpace(r.ExceptionMessage)
            && r.ActionResult?.Exception != null)
            Console.Write($"  !! {r.ExceptionMessage}");

        Console.WriteLine();

        if (r.ChildResults != null)
        {
            foreach (var child in r.ChildResults)
                PrintOne(child, indent + 1, includeFailed);
        }
    }

    private static string FormatOutput(object output) =>
        output switch
        {
            string s => $"\"{s}\"",
            IDictionary<string, object> dict =>
                "{ " + string.Join(", ", dict.Select(kv => $"{kv.Key}={kv.Value}")) + " }",
            _ => output.ToString() ?? ""
        };

    /// <summary>收集所有命中规则的 SuccessEvent（常用于促销叠加标签）。</summary>
    public static IReadOnlyList<string> CollectSuccessEvents(IEnumerable<RuleResultTree> results)
    {
        var list = new List<string>();
        void Walk(RuleResultTree r)
        {
            if (r.IsSuccess && !string.IsNullOrWhiteSpace(r.Rule.SuccessEvent))
                list.Add(r.Rule.SuccessEvent);
            if (r.ChildResults == null) return;
            foreach (var c in r.ChildResults) Walk(c);
        }

        foreach (var r in results) Walk(r);
        return list;
    }

    /// <summary>取第一条成功规则的 Action Output（审批路由等「只取最高优先级」场景）。</summary>
    public static object? FirstSuccessOutput(IEnumerable<RuleResultTree> results)
    {
        foreach (var r in Flatten(results))
        {
            if (r.IsSuccess && r.ActionResult?.Output is not null)
                return r.ActionResult.Output;
        }

        return null;
    }

    /// <summary>聚合所有成功规则的数值 Output（折扣率累乘 / 运费累加前需业务自行解释）。</summary>
    public static IReadOnlyList<object> AllSuccessOutputs(IEnumerable<RuleResultTree> results)
    {
        return Flatten(results)
            .Where(r => r.IsSuccess && r.ActionResult?.Output is not null)
            .Select(r => r.ActionResult!.Output!)
            .ToList();
    }

    private static IEnumerable<RuleResultTree> Flatten(IEnumerable<RuleResultTree> roots)
    {
        foreach (var r in roots)
        {
            yield return r;
            if (r.ChildResults == null) continue;
            foreach (var c in Flatten(r.ChildResults))
                yield return c;
        }
    }
}
