using RulesEngine_Demo.Approaches;
using RulesEngine_Demo.Approaches.CodeWorkflow;
using RulesEngine_Demo.Approaches.ConfigStore;
using RulesEngine_Demo.Approaches.CustomActions;
using RulesEngine_Demo.Approaches.DecisionTable;
using RulesEngine_Demo.Approaches.FluentBuilder;
using RulesEngine_Demo.Approaches.JsonWorkflow;
using RulesEngine_Demo.Approaches.YamlWorkflow;
using RulesEngine_Demo.Infrastructure;

Console.OutputEncoding = System.Text.Encoding.UTF8;

IDefinitionApproach[] approaches =
[
    new JsonDefinitionApproach(),
    new CodeDefinitionApproach(),
    new YamlDefinitionApproach(),
    new FluentDefinitionApproach(),
    new DecisionTableDefinitionApproach(),
    new CustomActionsDefinitionApproach(),
    new ConfigStoreDefinitionApproach()
];

// 非交互：dotnet run -- --all
if (args.Any(a => a.Equals("--all", StringComparison.OrdinalIgnoreCase)))
{
    ConsoleUi.Title("RulesEngine 定义方式演示（--all）");
    foreach (var a in approaches)
    {
        if (a is JsonDefinitionApproach json)
            await json.RunAllNonInteractiveAsync();
        else
            await a.RunAsync();
    }

    return;
}

ConsoleUi.Title("RulesEngine 规则定义方式演示控制台");
Console.WriteLine("当前仓库原先演示的是「JSON Workflow 文件定义」（菜单 [1]）。");
Console.WriteLine("其余菜单展示同一引擎下的其他定义/扩展方式；每种都有独立文件夹 + README.md。");
Console.WriteLine($"资源根: {RuleEngineHost.ApproachesRoot}");
Console.WriteLine("总览文档: RulesEngine_Demo/README.md");

while (true)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("请选择「规则定义方式」:");
    Console.ResetColor();
    foreach (var a in approaches)
    {
        Console.WriteLine($"  [{a.Key}] {a.Title}");
        ConsoleUi.Info($"      → {a.Summary}");
    }
    Console.WriteLine("  [A] 按顺序跑完全部方式（JSON 跳过热更新交互）");
    Console.WriteLine("  [Q] 退出");
    Console.Write("> ");

    var input = (Console.ReadLine() ?? "").Trim();
    if (input.Equals("Q", StringComparison.OrdinalIgnoreCase))
        break;
    if (string.IsNullOrEmpty(input))
        continue;

    if (input.Equals("A", StringComparison.OrdinalIgnoreCase))
    {
        foreach (var a in approaches)
        {
            if (a is JsonDefinitionApproach json)
                await json.RunAllNonInteractiveAsync();
            else
                await a.RunAsync();
        }

        ConsoleUi.Pause();
        continue;
    }

    var approach = approaches.FirstOrDefault(a => a.Key.Equals(input, StringComparison.OrdinalIgnoreCase));
    if (approach is null)
    {
        ConsoleUi.Warn("无效选项。");
        continue;
    }

    try
    {
        await approach.RunAsync();
    }
    catch (Exception ex)
    {
        ConsoleUi.Error($"执行失败: {ex.Message}");
        ConsoleUi.Info(ex.ToString());
        ConsoleUi.Pause();
    }
}

Console.WriteLine("再见。");
