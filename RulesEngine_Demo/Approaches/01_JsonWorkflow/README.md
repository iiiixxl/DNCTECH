# 01 — JSON Workflow 文件定义

## 1. 这是什么定义方式

把 RulesEngine 的 **`Workflow` / `Rule` 对象树序列化为 `.json` 文件**，启动或热更新时用 `System.Text.Json` 反序列化，再 `new RulesEngine(workflows)`。

这是 microsoft/RulesEngine **文档与社区最常见**的用法，也是本仓库**最初那套 Demo** 采用的方式。

表达式字段 `Expression` 是 **LambdaExpression 字符串**（底层 System.Linq.Dynamic.Core），例如：

```json
"Expression": "customer.Level == 3 && order.Amount >= 1000"
```

输入名 `customer` / `order` 必须与执行时 `RuleParameter("customer", obj)` 一致。

---

## 2. 业务痛点

| 痛点 | 表现 |
|------|------|
| 规则埋在代码 | 促销满减、审批阈值、风控倍数散落在 Service 的 `if/else` |
| 改规则成本高 | 改 0.95→0.90 也要发版、回归、找窗口期 |
| 无法解释结果 | 客服问「为什么便宜了 200」只能翻日志或猜 |
| 多人协作冲突 | 运营要改活动，开发排期；制度改审批链又要提需求 |

---

## 3. 解决了什么问题

- **规则外置**：业务阈值进 JSON，应用只负责装载、执行、解释输出  
- **可审计**：`SuccessEvent` 记录命中标签；`OutputExpression` 给出折扣/路由等结果  
- **可热更新**：改文件或推配置后重建引擎即可（见场景 6）  
- **结构表达力**：支持嵌套 `Operator: And/Or`、`LocalParams`/`GlobalParams`、`Actions`

---

## 4. 适用 / 不适用

**适用**

- 促销、审批路由、风控阈值、运费加价开关等 **变更频繁** 的规则  
- 需要配置中心、运营后台、多环境覆盖  

**不适用**

- 需要复杂算法、事务、外部 IO 的逻辑（应放 Custom Action 或应用层）  
- 对类型安全要求极高、几乎不变的合规硬规则（可考虑代码定义）  
- 超大规则集且无缓存/编译优化时，注意冷启动与内存

---

## 5. 代码对比

### 硬编码

```csharp
decimal Pay(CustomerInput c, OrderInput o)
{
    var amount = o.Amount;
    if (c.Level == MemberLevel.Vip) amount *= 0.85m;
    else if (c.Level == MemberLevel.Gold) amount *= 0.90m;
    if (o.Category == "Electronics" && o.Amount >= 2000 && !o.UsedCoupon)
        amount -= 200;
    return amount;
}
```

### JSON（本方式）

```json
{
  "WorkflowName": "OrderDiscount",
  "Rules": [
    {
      "RuleName": "VipMemberDiscount",
      "Expression": "customer.Level == 3",
      "SuccessEvent": "VipDiscount15Off",
      "Actions": {
        "OnSuccess": {
          "Name": "OutputExpression",
          "Context": { "Expression": "0.85" }
        }
      }
    }
  ]
}
```

### 装载与执行

```csharp
var workflows = JsonWorkflowLoader.LoadDirectory(rulesDir);
host.UseWorkflows(workflows);
var results = await host.ExecuteAsync(
    "OrderDiscount",
    new RuleParameter("customer", customer),
    new RuleParameter("order", order));
```

叠加策略（取最小折扣系数、累加满减）在 `ApproachHelpers.CalculatePayable`：**故意留在 C#**，避免 JSON 表达式膨胀成脚本语言。

---

## 6. 本 Demo 目录结构

```
01_JsonWorkflow/
├── README.md                 ← 本文件
├── JsonDefinitionApproach.cs ← 菜单入口 [1]
├── JsonWorkflowLoader.cs     ← 反序列化
├── IJsonBusinessDemo.cs
├── Demos/                    ← 6 个贴近真实业务的场景
│   ├── OrderDiscountDemo.cs
│   ├── ApprovalRoutingDemo.cs
│   ├── RiskControlDemo.cs
│   ├── ContractTermsDemo.cs
│   ├── ShippingFeeDemo.cs
│   └── HotReloadDemo.cs
└── Rules/
    ├── order-discount.json
    ├── approval-routing.json
    ├── risk-control.json
    ├── contract-terms.json
    └── shipping-fee.json
```

---

## 7. 怎么使用 Demo

```bash
cd RulesEngine_Demo
dotnet run
# 选 [1] JSON Workflow 文件定义
# 再选业务场景 [1]～[6]，或 [A] 全跑（跳过热更新）
```

热更新场景 `[6]`：按提示改 `Rules/order-discount.json` 里 Silver 的 `0.95`，保存后回车 Reload。

非交互：

```bash
dotnet run -- --all   # 会跑 JSON 全部业务场景（无热更新交互）
```

---

## 8. 核心实现说明（加载链路）

```
Rules/*.json
  → JsonWorkflowLoader.LoadDirectory
  → List<Workflow>
  → RuleEngineHost.UseWorkflows
  → RulesEngine.ExecuteAllRulesAsync
  → RuleResultTree（IsSuccess / SuccessEvent / ActionResult.Output）
  → 业务解释（应付金额 / 审批链 / Block|Review|Pass）
```

注意（v6）：

- 无 `Rule.Priority` 属性；审批路由用 `Properties.Priority` + 互斥表达式  
- 嵌套规则用 `"Operator": "And"` + 子 `Rules`  
- `CustomTypes` 注册了 `RuleMath`，运费续重可用 `RuleMath.Ceiling(...)`

---

## 9. 生产落地建议与坑

1. **JSON Schema 校验** 后再入库，防止坏规则拖垮进程  
2. **规则版本 + 输入快照** 落库，客诉可复盘  
3. 热更新要 **防抖 / 灰度**，表达式编译有成本  
4. 输出约定稳定契约（如路由节点码），不要输出人名  
5. Expression 里禁止随意调用危险 API；白名单类型用 `CustomTypes`  
6. 折扣「互斥 / 叠加」策略写在应用层并单测

---

## 10. 与其他定义方式的关系

| 方式 | 关系 |
|------|------|
| YAML | 同一对象模型，换序列化格式 |
| 代码 / Fluent | 跳过文件，直接 new Rule；适合强类型 |
| 决策表 | 表 → 编译成与本方式相同的 Workflow |
| CustomActions | JSON 里 `Actions.Name` 指向自研 Action |
| ConfigStore | JSON 字符串存 DB/配置中心，装载逻辑同本方式 |
