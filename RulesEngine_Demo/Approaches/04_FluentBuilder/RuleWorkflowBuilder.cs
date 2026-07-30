using RulesEngine.Models;

namespace RulesEngine_Demo.Approaches.FluentBuilder;

/// <summary>
/// Fluent API 构建 Workflow：
/// <code>
/// WorkflowBuilder.Create("Name")
///   .Rule("R1").When("expr").Event("E").Output("0.85").WithPriority(100).Add()
///   .Rule("R2").AndChild("c1","e1").AndChild("c2","e2").Event(...).Output("-200").Add()
///   .Build();
/// </code>
/// </summary>
public sealed class WorkflowBuilder
{
    private readonly string _workflowName;
    private readonly List<Rule> _rules = new();

    private WorkflowBuilder(string workflowName) => _workflowName = workflowName;

    public static WorkflowBuilder Create(string workflowName) => new(workflowName);

    public RuleBuilder Rule(string ruleName) => new(this, ruleName);

    public Workflow Build() => new()
    {
        WorkflowName = _workflowName,
        Rules = _rules.ToList()
    };

    internal void AddRule(Rule rule) => _rules.Add(rule);

    public sealed class RuleBuilder
    {
        private readonly WorkflowBuilder _owner;
        private readonly string _ruleName;
        private string? _expression;
        private string? _successEvent;
        private string? _outputExpression;
        private string? _priority;
        private readonly List<Rule> _children = new();

        internal RuleBuilder(WorkflowBuilder owner, string ruleName)
        {
            _owner = owner;
            _ruleName = ruleName;
        }

        public RuleBuilder When(string expression)
        {
            _expression = expression;
            return this;
        }

        public RuleBuilder Event(string successEvent)
        {
            _successEvent = successEvent;
            return this;
        }

        public RuleBuilder Output(string expression)
        {
            _outputExpression = expression;
            return this;
        }

        public RuleBuilder WithPriority(int priority)
        {
            _priority = priority.ToString();
            return this;
        }

        /// <summary>添加 And 子规则（RuleName + Expression）。</summary>
        public RuleBuilder AndChild(string childName, string childExpression)
        {
            _children.Add(new Rule
            {
                RuleName = childName,
                Expression = childExpression,
                Enabled = true
            });
            return this;
        }

        public WorkflowBuilder Add()
        {
            var rule = new Rule
            {
                RuleName = _ruleName,
                Enabled = true,
                Expression = _expression,
                SuccessEvent = _successEvent,
                Properties = _priority is null
                    ? null
                    : new Dictionary<string, object> { ["Priority"] = _priority }
            };

            if (_children.Count > 0)
            {
                rule.Operator = "And";
                rule.Rules = _children.ToList();
            }

            if (_outputExpression is not null)
            {
                rule.Actions = new RuleActions
                {
                    OnSuccess = new ActionInfo
                    {
                        Name = "OutputExpression",
                        Context = new Dictionary<string, object> { ["Expression"] = _outputExpression }
                    }
                };
            }

            _owner.AddRule(rule);
            return _owner;
        }
    }
}
