# 05 — 决策表（CSV）定义

## 1. 这是什么定义方式

用 **表格**（本 Demo 为 CSV，生产可换 Excel）描述「条件列 → 输出列」，运行时由 `DecisionTableCompiler` **编译成** RulesEngine 的 `Workflow`/`Rule`（拼出 `Expression` 与 `OutputExpression`）。

这是业务侧最容易接手的形态之一：改单元格，而不是改 JSON 树。

---

## 2. 业务痛点

- 会员档 × 折扣、金额档 × 审批链，本质是 **二维/多维矩阵**，用嵌套 if 或深 JSON 都不直观  
- BA/运营习惯 Excel，不愿碰花括号  
- 评审时需要「一眼看完所有档位」

---

## 3. 解决了什么问题

- 规则以表呈现，缺口/冲突更容易被肉眼发现  
- 编译后仍走标准 RulesEngine，不重复造执行器  
- 可对表做校验（档位重叠、空洞、优先级）

---

## 4. 适用 / 不适用

**适用**：档位型、矩阵型规则（折扣档、审批矩阵、税率档、运费分区码映射）。  
**不适用**：深嵌套逻辑、大量 Or/And 组合、需要引用中间变量的复杂图（更适合 JSON 嵌套规则或代码）。

---

## 5. 代码对比

### if-else 矩阵

```csharp
if (level == Vip) return 0.85m;
if (level == Gold) return 0.90m;
if (level == Silver) return 0.95m;
return 1.0m;
```

### CSV（本方式）

```csv
RuleName,MemberLevel,MinAmount,DiscountFactor,SuccessEvent,Priority
VipDiscount,3,0,0.85,VipDiscount15Off,100
GoldDiscount,2,0,0.90,GoldDiscount10Off,90
SilverDiscount,1,0,0.95,SilverDiscount5Off,80
```

### 编译结果（等价 Expression）

```text
customer.Level == 3 && order.Amount >= 0
→ OutputExpression 0.85
```

### 审批表

```csv
RuleName,MinAmount,MaxAmount,ExpenseType,VendorRisk,Route,SuccessEvent,Priority
SmallAmountManager,0,20000,,,DEPT_MANAGER,RouteManager,100
```

编译为：`req.Amount >= 0 && req.Amount < 20000`，输出 `"DEPT_MANAGER"`。

---

## 6. 目录结构

```
05_DecisionTable/
├── README.md
├── DecisionTableDefinitionApproach.cs
├── DecisionTableCompiler.cs
└── Tables/
    ├── member-discount.csv
    └── approval-matrix.csv
```

---

## 7. 怎么使用

```bash
dotnet run
# 选 [5]
```

改 CSV 后重新运行（或自行加 FileSystemWatcher 热编译）。注意 **输出目录** 会复制 Tables；改源码树下的 CSV 后需重新 build/run。

---

## 8. 核心实现说明

```
CSV 行
  → 按列拼 Expression（AND）
  → Priority 写入 Properties
  → Route/DiscountFactor → OutputExpression
  → Workflow
```

`ReadCsv` 支持引号字段；表头会去掉 UTF-8 BOM。

---

## 9. 生产建议与坑

1. **重叠档位**：两行同时命中时要用 Priority 或编译期冲突检测  
2. 空单元格语义要文档化（空 = 不限制该维度）  
3. Excel 导出 CSV 注意分隔符/本地化小数点  
4. 建议后台「表编辑 UI」→ 存表 → 编译 → 发布，而不是让人直接改生产文件  

---

## 10. 与其他方式的关系

决策表是 **定义 UX**；落地对象与 **01 JSON** 相同。可把编译结果缓存为 JSON 再交给 **07 ConfigStore**。
