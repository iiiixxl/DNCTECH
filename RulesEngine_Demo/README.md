# RulesEngine_Demo — 规则定义方式总览

## 一句话结论

**原先那套演示用的是「JSON Workflow 文件定义」**：把 `Workflow` / `Rule` 写成 `.json`，运行时反序列化后交给 [microsoft/RulesEngine](https://github.com/microsoft/RulesEngine)。

RulesEngine **真正执行的对象模型**始终是内存里的 `Workflow` + `Rule`（表达式类型在 v6 仅有 `LambdaExpression`）。  
JSON / YAML / 代码 / Fluent / 决策表 / 配置库，都是**如何把规则「定义并装载」成这棵对象树**的差异；自定义 Action 则是**命中后副作用**的扩展。

```
定义载体（JSON/YAML/CSV/C#/DB）
        │  装载 / 编译
        ▼
   Workflow[] 对象树
        │  new RulesEngine(workflows, settings)
        ▼
 ExecuteAllRulesAsync(input) → RuleResultTree + Action Output
```

---

## 七种方式怎么选

| 菜单 | 文件夹 | 定义方式 | 谁改规则 | 要不要发版 | 典型场景 |
|------|--------|----------|----------|------------|----------|
| **1** | `01_JsonWorkflow` | **JSON 文件**（官方主流） | 运营/配置同学 + Schema 校验 | 通常否 | 促销、审批、风控阈值 |
| **2** | `02_CodeWorkflow` | **C# 对象树** | 开发 | 是 | 强约束、随版本发布的合规规则 |
| **3** | `03_YamlWorkflow` | **YAML 文件** | 同 JSON | 通常否 | 嫌 JSON 括号多、Git diff 友好 |
| **4** | `04_FluentBuilder` | **Fluent API** | 开发 | 是 | 代码里可读地拼规则，仍编译进程序集 |
| **5** | `05_DecisionTable` | **决策表 CSV** | 业务/BA 填表 | 否（改表热加载亦可） | 会员档、审批矩阵、费率档 |
| **6** | `06_CustomActions` | **自定义 Action**（扩展执行） | 开发写 Action + 配置 Name | Action 要发版 | 审计、通知、写库、复杂计价 |
| **7** | `07_ConfigStore` | **配置库/规则商店**（存储位置） | 后台/配置中心 | 否 | 多环境、版本灰度、热更新 |

> **说明**：6 不是另一种「文件格式」，而是执行侧扩展；7 不是另一种 DSL，而是规则**存放与热更新**模式。二者常与 1/3/5 组合使用。

---

## 目录地图

```
RulesEngine_Demo/
├── README.md                          ← 本文件
├── Program.cs                         ← 按「定义方式」选菜单
├── Models/                            ← 共享业务输入模型
├── Infrastructure/                    ← RuleEngineHost / ConsoleUi / RuleMath
└── Approaches/
    ├── IDefinitionApproach.cs
    ├── ApproachHelpers.cs
    ├── 01_JsonWorkflow/               ← JSON + 6 个业务场景 + README
    ├── 02_CodeWorkflow/
    ├── 03_YamlWorkflow/
    ├── 04_FluentBuilder/
    ├── 05_DecisionTable/
    ├── 06_CustomActions/
    └── 07_ConfigStore/
```

每种方式目录内都有 **`README.md`**，固定包含：痛点、解决了什么、适用/不适用、代码对比、目录结构、怎么跑、实现链路、生产建议。

---

## 怎么运行

```bash
cd RulesEngine_Demo
dotnet run
```

交互菜单选 `[1]`～`[7]`。

一次性跑完全部（JSON 跳过热更新人工交互）：

```bash
dotnet run -- --all
```

详细文档路径提示会出现在每个 Demo 开头（`Approaches/0x_xxx/README.md`）。

---

## 与「硬编码 if-else」的共性对比

```csharp
// 痛点：阈值、互斥、优先级全埋在代码里；改一次发一次版；无法审计「命中了哪条」
if (customer.Level == MemberLevel.Vip)
    amount *= 0.85m;
else if (customer.Level == MemberLevel.Gold)
    amount *= 0.90m;
if (order.Category == "Electronics" && order.Amount >= 2000 && !order.UsedCoupon)
    amount -= 200;
```

引入 RulesEngine 后：

- **条件与输出**外置（或集中在 Factory/表）
- **SuccessEvent** 留下命中标签，便于对账与客服
- **叠加策略**（取最优折扣 / 累加满减）仍建议留在应用代码，避免表达式变成第二套编程语言

---

## 推荐组合（生产）

1. **默认**：`JSON 或 YAML` + `配置中心/DB（方式 7）` + Schema 校验 + 规则版本快照  
2. **档位型业务**：决策表 CSV/Excel 给业务填，编译成 Workflow（方式 5）  
3. **副作用**：Custom Action（方式 6），不要把 HTTP/发邮件写进 Expression  
4. **强合规、极少变更**：代码定义或 Fluent（方式 2/4），走 Code Review  

---

## 技术备注（RulesEngine 6）

- `RuleExpressionType` 目前实质为 **LambdaExpression**（Dynamic LINQ 字符串）
- 已无内置 `Rule.Priority`，可用 `Properties["Priority"]` 或互斥表达式
- `ReSettings.CustomTypes` 注册 `RuleMath` 等辅助类型
- `ReSettings.CustomActions` + `AutoExecuteActions = true` 启用自定义动作
