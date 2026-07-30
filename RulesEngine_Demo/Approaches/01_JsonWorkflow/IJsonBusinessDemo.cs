using RulesEngine_Demo.Infrastructure;

namespace RulesEngine_Demo.Approaches.JsonWorkflow;

/// <summary>JSON 业务场景子菜单共用接口。</summary>
public interface IJsonBusinessDemo
{
    string Key { get; }
    string Title { get; }
    string Description { get; }
    Task RunAsync(RuleEngineHost host, string rulesDirectory);
}
