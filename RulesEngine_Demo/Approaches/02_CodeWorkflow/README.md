# 02 — C# 代码定义 Workflow

## 1. 这是什么定义方式

不经过 JSON/YAML 文件，在 C# 里直接：

```csharp
new Workflow {
  WorkflowName = "OrderDiscount",
  Rules = new List<Rule> { new Rule { RuleName = "...", Expression = "...", ... } }
}
```

最终仍交给同一个 `RulesEngine`。本 Demo 集中在 `CodeWorkflowFactory`。

---

## 2. 业务痛点

- 部分规则 **必须随版本发布**（金融合规、硬性风控底线），不允许运营后台随意改  
- 纯 JSON 缺少编译期检查，重构输入模型时字符串表达式易漏改  
- 希望规则变更走 **PR / Code Review**，而不是配置中心点一点

---

## 3. 解决了什么问题

- 与领域模型同仓、可重构（改名可借助编译器/分析器）  
- 复杂嵌套规则用对象初始化器，IDE 可导航  
- 仍然复用 RulesEngine 的执行、Action、SuccessEvent 能力  

---

## 4. 适用 / 不适用

**适用**：强合规、低变更频率、必须 Code Review 的规则包。  
**不适用**：天天改的促销文案式阈值（应用 JSON/YAML/配置库更合适）。

---

## 5. 代码对比

### JSON

```json
"Expression": "customer.Level == 3"
```

### 代码（本方式）

```csharp
new Rule
{
    RuleName = "VipMemberDiscount",
    Expression = "customer.Level == 3",
    SuccessEvent = "VipDiscount15Off",
    Properties = new Dictionary<string, object> { ["Priority"] = "100" },
    Actions = new RuleActions
    {
        OnSuccess = new ActionInfo
        {
            Name = "OutputExpression",
            Context = new Dictionary<string, object> { ["Expression"] = "0.85" }
        }
    }
}
```

注意：`Expression` **仍是字符串**（LambdaExpression），并不是 C# 表达式树 API。若要真正的强类型表达式树，需自研或换引擎。

---

## 6. 目录结构

```
02_CodeWorkflow/
├── README.md
├── CodeDefinitionApproach.cs   ← 菜单 [2]
└── CodeWorkflowFactory.cs      ← 纯代码组装 OrderDiscount + ApprovalRouting
```

---

## 7. 怎么使用

```bash
dotnet run
# 选 [2]
```

会跑若干订单促销用例 + 审批路由用例，并打印命中与应付金额。

---

## 8. 核心实现说明

```
CodeWorkflowFactory.CreateAll()
  → Workflow[]
  → host.UseWorkflows(...)
  → ExecuteAllRulesAsync
```

与 JSON 方式的唯一差别在 **装载源头**。

---

## 9. 生产建议与坑

- 把「可变阈值」抽成 `const`/Options，避免魔法数散落  
- 大规则集可按限界上下文拆多个 Factory  
- 不要以为代码定义就没有字符串 Expression 的测试负担——**单测同样要覆盖**

---

## 10. 与其他方式的关系

常与 **Fluent（04）** 一起：Fluent 是更可读的代码定义糖。  
与 **JSON（01）** 可混合：底线规则代码写死，活动规则 JSON 外置。
