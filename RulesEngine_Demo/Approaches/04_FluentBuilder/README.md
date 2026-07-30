# 04 — Fluent Builder 定义

## 1. 这是什么定义方式

在代码定义（02）之上，提供可读的链式 API：

```csharp
WorkflowBuilder.Create("OrderDiscount")
  .Rule("VipMemberDiscount")
    .When("customer.Level == 3")
    .Event("VipDiscount15Off")
    .Output("0.85")
    .WithPriority(100)
    .Add()
  .Build();
```

本质仍是组装 `Workflow`/`Rule`，**不引入新的规则 DSL 文件格式**。

---

## 2. 业务痛点

- `new Rule { ... Actions = new RuleActions { ... } }` 样板代码吵闹  
- 嵌套 And 子规则时对象初始化器层次深，难读  
- 希望规则「看起来像规格说明」，但仍在 C# 里

---

## 3. 解决了什么问题

- 提高代码定义的可读性与一致性（Priority、Output、Event 统一入口）  
- And 子规则用 `.AndChild("name", "expr")` 表达更直观  

---

## 4. 适用 / 不适用

**适用**：中等规模、以开发维护为主的规则包。  
**不适用**：业务人员直接改规则（他们需要 JSON/YAML/决策表/后台 UI）。

---

## 5. 代码对比

### 原始对象初始化 vs Fluent

```csharp
// 吵闹
new Rule {
  RuleName = "ElectronicsFullReduce",
  Operator = "And",
  Rules = new[] {
    new Rule { RuleName = "IsElectronics", Expression = "order.Category == \"Electronics\"" },
    new Rule { RuleName = "AmountOver2000", Expression = "order.Amount >= 2000" }
  },
  Actions = new RuleActions { OnSuccess = new ActionInfo { Name = "OutputExpression", Context = ... } }
}

// Fluent
.Rule("ElectronicsFullReduce")
  .AndChild("IsElectronics", "order.Category == \"Electronics\"")
  .AndChild("AmountOver2000", "order.Amount >= 2000")
  .AndChild("NotUsingCoupon", "order.UsedCoupon == false")
  .Event("ElectronicsMinus200")
  .Output("-200")
  .Add()
```

---

## 6. 目录结构

```
04_FluentBuilder/
├── README.md
├── FluentDefinitionApproach.cs
└── RuleWorkflowBuilder.cs   ← WorkflowBuilder / RuleBuilder
```

---

## 7. 怎么使用

```bash
dotnet run
# 选 [4]
```

---

## 8. 核心实现说明

`RuleBuilder.Add()` 把累积的 Expression/Children/Actions 写成一个 `Rule`，压入 `WorkflowBuilder` 列表，`Build()` 产出 `Workflow`。

---

## 9. 生产建议与坑

- Fluent 只是语法糖，**Expression 字符串依旧要单测**  
- 团队应约定 Builder 能力边界（不要做成另一门语言）  
- 可与源代码生成结合（从决策表生成 Fluent 调用）——进阶玩法

---

## 10. 与其他方式的关系

是 **02 代码定义** 的可读封装；执行期与 JSON 完全一致。
