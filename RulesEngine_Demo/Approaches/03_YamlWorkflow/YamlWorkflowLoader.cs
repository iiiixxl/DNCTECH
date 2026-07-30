using RulesEngine.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RulesEngine_Demo.Approaches.YamlWorkflow;

/// <summary>用 YamlDotNet 将 YAML 反序列化为 RulesEngine Workflow。</summary>
public static class YamlWorkflowLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance) // YAML 使用 PascalCase，与模型字段一致
        .IgnoreUnmatchedProperties()
        .Build();

    public static List<Workflow> LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(directory);

        var list = new List<Workflow>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.yaml")
                     .Concat(Directory.EnumerateFiles(directory, "*.yml"))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            list.AddRange(LoadFile(file));
        return list;
    }

    public static IEnumerable<Workflow> LoadFile(string path)
    {
        var yaml = File.ReadAllText(path);
        var trimmed = yaml.TrimStart();

        // 列表：- WorkflowName: ...
        if (trimmed.StartsWith('-'))
        {
            var dtos = Deserializer.Deserialize<List<YamlWorkflowDto>>(yaml)
                       ?? throw new InvalidOperationException($"无法解析 YAML 列表: {path}");
            return dtos.Select(MapWorkflow);
        }

        var single = Deserializer.Deserialize<YamlWorkflowDto>(yaml)
                     ?? throw new InvalidOperationException($"无法解析 YAML: {path}");
        return [MapWorkflow(single)];
    }

    private static Workflow MapWorkflow(YamlWorkflowDto dto) => new()
    {
        WorkflowName = dto.WorkflowName ?? throw new InvalidOperationException("WorkflowName 缺失"),
        Rules = (dto.Rules ?? []).Select(MapRule).ToList()
    };

    private static Rule MapRule(YamlRuleDto dto)
    {
        var rule = new Rule
        {
            RuleName = dto.RuleName ?? "",
            Enabled = dto.Enabled ?? true,
            Expression = dto.Expression,
            SuccessEvent = dto.SuccessEvent,
            Operator = dto.Operator,
            Properties = dto.Properties is null
                ? null
                : dto.Properties.ToDictionary(kv => kv.Key, kv => (object)kv.Value),
            Rules = dto.Rules is { Count: > 0 } ? dto.Rules.Select(MapRule).ToList() : null
        };

        if (dto.Actions?.OnSuccess is { } onSuccess)
        {
            rule.Actions = new RuleActions
            {
                OnSuccess = new ActionInfo
                {
                    Name = onSuccess.Name ?? "OutputExpression",
                    Context = onSuccess.Context is null
                        ? new Dictionary<string, object>()
                        : onSuccess.Context.ToDictionary(kv => kv.Key, kv => (object)kv.Value)
                }
            };
        }

        return rule;
    }

    // —— YAML DTO：Context/Properties 用 string 值，避免 Dictionary&lt;string, object&gt; 反序列化歧义 ——

    private sealed class YamlWorkflowDto
    {
        public string? WorkflowName { get; set; }
        public List<YamlRuleDto>? Rules { get; set; }
    }

    private sealed class YamlRuleDto
    {
        public string? RuleName { get; set; }
        public bool? Enabled { get; set; }
        public string? Expression { get; set; }
        public string? SuccessEvent { get; set; }
        public string? Operator { get; set; }
        public Dictionary<string, string>? Properties { get; set; }
        public List<YamlRuleDto>? Rules { get; set; }
        public YamlActionsDto? Actions { get; set; }
    }

    private sealed class YamlActionsDto
    {
        public YamlActionInfoDto? OnSuccess { get; set; }
    }

    private sealed class YamlActionInfoDto
    {
        public string? Name { get; set; }
        public Dictionary<string, string>? Context { get; set; }
    }
}
