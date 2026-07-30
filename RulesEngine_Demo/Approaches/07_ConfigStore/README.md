# 07 — 配置库 / 规则商店（存储 + 热更新）

## 1. 这是什么定义方式

关注点从「文件格式」转向 **规则存放在哪里、如何变更生效**：

- 用 `InMemoryRuleStore` **模拟 DB / 配置中心**  
- 存的是 Workflow 的 JSON 文本 + 版本号  
- `Upsert` 后重新反序列化并 `UseWorkflows`，实现 **不重启进程的热更新**

内容格式仍可以是 JSON（本 Demo）；也可以换成 YAML 字符串。

---

## 2. 业务痛点

- 规则放磁盘文件：多实例一致性差、缺权限审计、难灰度  
- 改文件依赖运维发布或共享盘，无法「后台点保存即生效」  
- 出事故无法按版本回滚；客诉无法对齐「当时用的哪一版规则」

---

## 3. 解决了什么问题

- 统一的 **读写 API**（GetAllJson / Upsert / GetVersion）  
- 演示「改库 → 重建引擎 → 第二次执行结果变化」  
- 为接 Nacos / App Configuration / 自研规则中心留下相同边界

---

## 4. 适用 / 不适用

**适用**：多实例、要审计、要回滚、要按租户覆盖规则。  
**不适用**：单机学习 Demo、完全静态的内嵌规则（用 02 即可）。

---

## 5. 代码对比

### 文件热更新（01 场景 6）

```text
改 order-discount.json → LoadDirectory → UseWorkflows
```

### 配置库（本方式）

```csharp
store.Upsert("OrderDiscount", jsonWithSilver095);
LoadFromStore(host, store);   // 第一次：0.95
store.Upsert("OrderDiscount", jsonWithSilver090);
LoadFromStore(host, store);   // 第二次：0.90，版本 +1
```

对业务调用方而言，**仍然是 ExecuteAsync**，无感知存储迁移。

---

## 6. 目录结构

```
07_ConfigStore/
├── README.md
├── ConfigStoreDefinitionApproach.cs
└── InMemoryRuleStore.cs
```

种子 JSON 内联在 Approach 中，便于单文件理解；生产应来自 DB。

---

## 7. 怎么使用

```bash
dotnet run
# 选 [7]
```

你会看到：

1. 第一次 Silver 折扣 Output = 0.95  
2. Upsert 后版本号增加  
3. 第二次 Output = 0.90  

无需手工改文件。

---

## 8. 核心实现说明

```
InMemoryRuleStore (dict + version)
  → GetAllJson()
  → JsonSerializer.Deserialize<Workflow>
  → host.UseWorkflows
  → Execute
```

并发下应用 **版本 CAS / 乐观锁**；多实例用消息总线通知 Reload。

---

## 9. 生产建议与坑

1. 写入前 **Schema + 试跑一套回归用例**（金丝雀）  
2. 保存 **规则版本、生效时间、操作者、变更 diff**  
3. 执行日志带 `ruleVersion`，与订单快照一起存  
4. 重建引擎有成本：防抖、按 Workflow 增量替换  
5. 多租户：Store Key = `tenantId + workflowName`  

---

## 10. 与其他方式的关系

| 组合 | 含义 |
|------|------|
| 07 + 01/03 | 库里存 JSON/YAML 文本 |
| 07 + 05 | 库里存 CSV/表，读取时编译 |
| 07 + 06 | 库改参数，Action 代码发版 |

这是 **生产落地几乎必做的一层**，与「用哪种文件格式」正交。
