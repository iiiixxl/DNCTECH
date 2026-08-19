# SessionExpiredRace_Demo（伪 AbpSessions 入库时序竞态）

这个 Demo 用“伪事务/伪 AbpSessions 表”模拟你要理解的两个现象：

1. **主动取消/回滚**：响应已经发给客户端了，但这次请求结束前事务没提交（最终回滚），下一请求查库找不到。
2. **提前写出响应**：响应先写出，客户端立刻认为完成并发起下一请求；但事务还没等到 endpoint 返回后才 commit，所以下一请求查库找不到。

你只需要观察：同一个 `session_id` 在“立即查询”和“等待一会儿再查询”之间是否从 404 变成 200。

---

## 运行

```bash
dotnet run --project "./SessionExpiredRace_Demo.csproj" -c Debug
```

默认启动端口一般是 `http://localhost:<port>`（控制台会打印）。

---

## 接口说明

### 1) 清空伪表

`POST /reset`

返回 `200 OK`。

---

### 2) 模拟场景 1：主动回滚导致“永远找不到”

`POST /race/cancel-before-commit?cancelAfterMs=50&returnAfterMs=1500`

- 会返回一个 JSON：
  - `access_token`（假 token）
  - `session_id`（你要拿去查库）
  - `scenario`
- **返回给客户端的那一刻**：session 已经被“stage insert”（存在于当前请求的事务里），但还没 commit。
- **接着 cancelAfterMs 之后**：标记事务需要回滚。
- **middleware 真正回滚**：要等 endpoint 返回后才发生。

因此你会看到：

- 响应回来后**立刻**调用 `GET /session/{id}`：404
- 等很久再调用 `GET /session/{id}`：仍然 404（因为这一请求最终回滚了）

---

### 3) 模拟场景 2：提前写出响应导致“先查不到，等 commit 后查得到”

`POST /race/early-write?delayBeforeCommitMs=1500`

- 同样会返回 JSON，包含 `session_id`
- **响应 flush 之后**：endpoint 会 `Delay`，让事务保持“未 commit”状态
- **直到 endpoint 返回**：middleware 才 commit

因此你会看到：

- 响应回来后**立刻**调用 `GET /session/{id}`：404
- 等 `delayBeforeCommitMs` 以上时间后再调用 `GET /session/{id}`：200

---

### 4) 查“伪 AbpSessions”

`GET /session/{id}`

- 查到：`200`，返回 `exists=true`
- 查不到：`404`，返回 `exists=false`

---

### 5) 观察已 commit 的行（辅助）

`GET /debug/committed`

返回当前伪表里已经 commit 的 session 列表。

---

## 观察点（最关键）

看服务端日志里这些标记：

- `[UOW] BEGIN`：一次请求开始（开始一个“事务上下文”）
- `[SC1]` 或 `[SC2]`：stage insert、response flush 的时间点
- `[UOW] COMMIT` / `[UOW] ROLLBACK`：事务真正落库/回滚的时间点

日志顺序对应你要理解的那句：

> 客户端认为“完成” != 服务器事务已经 commit/落库

