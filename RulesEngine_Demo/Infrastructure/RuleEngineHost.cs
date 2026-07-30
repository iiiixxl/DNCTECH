using RulesEngine.Models;

namespace RulesEngine_Demo.Infrastructure;

/// <summary>统一的 RulesEngine 宿主：任意定义方式最终都变成 Workflow[] 交给它。</summary>
public sealed class RuleEngineHost
{
    private RulesEngine.RulesEngine _engine;
    private readonly ReSettings _settings;

    public IReadOnlyList<Workflow> Workflows { get; private set; } = Array.Empty<Workflow>();

    public RuleEngineHost(ReSettings? settings = null)
    {
        _settings = settings ?? CreateDefaultSettings();
        _engine = new RulesEngine.RulesEngine(Array.Empty<Workflow>(), _settings);
    }

    public static ReSettings CreateDefaultSettings(Dictionary<string, Func<RulesEngine.Actions.ActionBase>>? customActions = null)
    {
        return new ReSettings
        {
            NestedRuleExecutionMode = NestedRuleExecutionMode.Performance,
            CustomTypes = [typeof(RuleMath)],
            CustomActions = customActions,
            // v6：显式开启，确保 OnSuccess/OnFailure Action 被执行
            AutoExecuteActions = true
        };
    }

    public void UseWorkflows(IEnumerable<Workflow> workflows, ReSettings? settings = null)
    {
        var list = workflows.ToList();
        Workflows = list;
        var s = settings ?? _settings;
        _engine = new RulesEngine.RulesEngine(list.ToArray(), s);
    }

    public RulesEngine.RulesEngine Engine => _engine;

    public async Task<List<RuleResultTree>> ExecuteAsync(string workflowName, params RuleParameter[] parameters)
        => await _engine.ExecuteAllRulesAsync(workflowName, parameters);

    public async Task<List<RuleResultTree>> ExecuteAsync(string workflowName, params object[] inputs)
        => await _engine.ExecuteAllRulesAsync(workflowName, inputs);

    /// <summary>各 Approach 资源根：bin/.../Approaches</summary>
    public static string ApproachesRoot =>
        Path.Combine(AppContext.BaseDirectory, "Approaches");

    public static string ApproachPath(string folderName) =>
        Path.Combine(ApproachesRoot, folderName);
}
