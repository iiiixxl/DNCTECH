# 06 — 自定义 Action（CustomActions）

## 1. 这是什么定义方式

严格说它不是「另一种规则文件格式」，而是 **规则命中后的执行扩展**：

- 规则仍可用 JSON/YAML/代码定义  
- `Actions.OnSuccess.Name` 不再用内置 `OutputExpression`，而指向你注册的 **`ActionBase` 实现**  
- 在 `ReSettings.CustomActions` 里：`["ApplyDiscount"] = () => new ApplyDiscountAction()`

本 Demo 用 JSON 定义规则 + 两个自定义 Action：`ApplyDiscount`、`SendAuditLog`。

---

## 2. 业务痛点

- `OutputExpression` 只能算个值，不能 **写审计、发消息、调领域服务**  
- 把副作用塞进 Expression 字符串：难测、难控权、难排查  
- 希望规则文件只描述「何时触发」，副作用在强类型 C# 中实现

---

## 3. 解决了什么问题

- **关注点分离**：条件在规则，动作在 Action 类  
- Action 可注入服务（生产中用工厂解析 DI）  
- 演示审计队列：命中 VIP 时写入 `AuditSink`，便于合规留痕

---

## 4. 适用 / 不适用

**适用**：审计、通知、指标打点、调用计价服务、写 outbox。  
**不适用**：纯粹返回一个系数（用 `OutputExpression` 即可，更简单）。

---

## 5. 代码对比

### 仅 OutputExpression

```json
"Actions": {
  "OnSuccess": {
    "Name": "OutputExpression",
    "Context": { "Expression": "0.85" }
  }
}
```

### 自定义 Action（本方式）

```json
"Actions": {
  "OnSuccess": {
    "Name": "ApplyDiscount",
    "Context": { "Factor": "0.85" }
  }
}
```

```csharp
public sealed class ApplyDiscountAction : ActionBase
{
    public override ValueTask<object> Run(ActionContext context, RuleParameter[] ruleParameters)
    {
        if (context.TryGetContext<string>("Factor", out var f))
            return new ValueTask<object>($"discount:{f}");
        ...
    }
}

// 注册
CustomActions = new Dictionary<string, Func<ActionBase>> {
  ["ApplyDiscount"] = () => new ApplyDiscountAction(),
  ["SendAuditLog"] = () => new SendAuditLogAction()
};
```

**Name 字符串必须与字典 Key 完全一致。**

---

## 6. 目录结构

```
06_CustomActions/
├── README.md
├── CustomActionsDefinitionApproach.cs
├── Actions/
│   ├── ApplyDiscountAction.cs
│   └── SendAuditLogAction.cs
└── Rules/
    └── order-discount-actions.json
```

---

## 7. 怎么使用

```bash
dotnet run
# 选 [6]
```

观察规则输出中的 `discount:` / `subtract:`，以及文末 **AuditSink** 队列。

---

## 8. 核心实现说明

```
JSON Name=ApplyDiscount
  → ReSettings.CustomActions["ApplyDiscount"]
  → ActionBase.Run
  → ActionResult.Output
```

宿主里设置了 `AutoExecuteActions = true`，确保 Action 被执行。

---

## 9. 生产建议与坑

1. Action **尽量无共享可变静态状态**（Demo 的 ConcurrentQueue 仅供展示）  
2. 工厂内解析 `IServiceScope`，避免 Captive Dependency  
3. Action 失败策略：`EnableExceptionAsErrorMessage` / 重试 / 降级要设计清楚  
4. 不要在 Action 里开长事务；改用 outbox  
5. 单元测试：直接测 Action.Run + 用固定 Context

---

## 10. 与其他方式的关系

可挂在 **01/03/02/04/05** 任一装载结果上。  
常与 **07** 一起：配置中心改 Context 参数，Action 程序集版本化发布。
