namespace RulesEngine_Demo.Approaches;

/// <summary>一种「规则定义方式」的入口（JSON / 代码 / YAML / Fluent / 决策表 / …）。</summary>
public interface IDefinitionApproach
{
    string Key { get; }
    string Title { get; }
    string Folder { get; }
    string Summary { get; }
    Task RunAsync();
}
