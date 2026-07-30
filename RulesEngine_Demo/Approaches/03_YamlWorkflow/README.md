# 03 — YAML Workflow 文件定义

## 1. 这是什么定义方式

对象模型与 JSON **完全相同**（`Workflow` / `Rule`），只是用 **YAML** 书写，经 YamlDotNet 反序列化（本 Demo 先落到 DTO 再 Map，以兼容嵌套 Actions）。

适合讨厌 JSON 括号、希望 Git diff 更清晰的团队。

---

## 2. 业务痛点

- JSON 嵌套深时难读、难改、易漏逗号  
- 配置同学更熟悉 YAML（K8s、CI 同款）  
- 希望与 JSON 方案 **共用同一套执行引擎与业务解释代码**

---

## 3. 解决了什么问题

- 可读性、注释友好（YAML 天然支持 `#` 注释；JSON 需靠 serializer 扩展）  
- 装载后与 JSON 路径汇合，无两套引擎  

---

## 4. 适用 / 不适用

**适用**：文件型配置、GitOps 管理规则。  
**不适用**：前端/浏览器侧直接编辑（JSON 更普遍）；或已有统一 JSON Schema 体系时不必强行换 YAML。

---

## 5. 代码对比

### JSON 片段

```json
{
  "RuleName": "SilverMemberDiscount",
  "Expression": "customer.Level == 1",
  "Actions": {
    "OnSuccess": {
      "Name": "OutputExpression",
      "Context": { "Expression": "0.95" }
    }
  }
}
```

### YAML（本方式）

```yaml
RuleName: SilverMemberDiscount
Enabled: true
Expression: customer.Level == 1
SuccessEvent: SilverDiscount5Off
Actions:
  OnSuccess:
    Name: OutputExpression
    Context:
      Expression: "0.95"
```

### 装载

```csharp
var workflows = YamlWorkflowLoader.LoadDirectory(rulesDir);
host.UseWorkflows(workflows);
```

---

## 6. 目录结构

```
03_YamlWorkflow/
├── README.md
├── YamlDefinitionApproach.cs
├── YamlWorkflowLoader.cs
└── Rules/
    ├── order-discount.yaml
    └── approval-routing.yaml
```

---

## 7. 怎么使用

```bash
dotnet run
# 选 [3]
```

---

## 8. 核心实现说明

```
*.yaml → YamlDotNet → YamlWorkflowDto → Map → Workflow → RulesEngine
```

本 Demo 使用 `NullNamingConvention`（YAML 字段 PascalCase 对齐模型）。若你更喜欢 camelCase，可改 NamingConvention 并统一文件风格。

---

## 9. 生产建议与坑

- 与 JSON 二选一作为 **唯一源格式**，避免双写漂移  
- 缩进错误是 YAML 最大坑，CI 里做 parse 校验  
- 多行表达式注意引号，避免被解析成结构

---

## 10. 与其他方式的关系

与 **01 JSON** 等价替换；可同样接到 **07 ConfigStore**（库里存 YAML 文本亦可）。
