using System.Text;
using System.Text.Json;
using SessionExpiredRace_Demo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 模拟：UoW 在 endpoint 返回后才 commit/rollback（对应 ABP 的 CompleteAsync）
app.UseMiddleware<FakeUnitOfWorkMiddleware>();

// 你可以用浏览器/Swagger 看接口，但 demo 关键是“并发/时间窗”。
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/reset", () =>
{
    FakeSessionStore.Clear();
    return Results.Ok(new { ok = true });
});

app.MapGet("/session/{id:guid}", (Guid id) =>
{
    if (FakeSessionStore.TryGet(id, out var row))
    {
        return Results.Ok(new
        {
            exists = true,
            session_id = row.SessionId,
            committedAtUtc = row.CreatedAtUtc
        });
    }

    return Results.NotFound(new
    {
        exists = false,
        session_id = id
    });
});

app.MapGet("/debug/committed", () =>
{
    var rows = FakeSessionStore.GetAll().ToArray();
    return Results.Ok(new { count = rows.Length, rows });
});

static async Task WriteJsonAndFlush(HttpContext context, object payload, CancellationToken ct)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var json = JsonSerializer.Serialize(payload);
    var bytes = Encoding.UTF8.GetBytes(json);
    context.Response.ContentLength = bytes.Length;

    await context.Response.Body.WriteAsync(bytes, ct);
    await context.Response.Body.FlushAsync(ct);
}

// 场景 1：
// 1) 写出响应（客户端拿到 session_id 认为成功）
// 2) 仍保持请求“没结束”，中间件此时还没 commit
// 3) 之后我们“主动回滚”（等效于 RequestAborted 导致 CompleteAsync 失败回滚）
// 4) 下一请求立刻查库：找不到（并且之后也不会出现）
app.MapPost("/race/cancel-before-commit", async (HttpContext context, int cancelAfterMs = 50, int returnAfterMs = 1500) =>
{
    var logger = app.Logger;
    var tx = context.Items[FakeUnitOfWorkMiddleware.ItemsKeyTx] as FakeTransaction
             ?? throw new InvalidOperationException("Missing fake tx in Items.");

    cancelAfterMs = Math.Max(0, cancelAfterMs);
    returnAfterMs = Math.Max(cancelAfterMs + 1, returnAfterMs);

    var sessionId = Guid.NewGuid();
    tx.StageInsert(sessionId);

    logger.LogInformation("[SC1] Staged Insert session_id={SessionId} traceId={TraceId}", sessionId, context.TraceIdentifier);

    // 先让客户端认为“登录完成”
    await WriteJsonAndFlush(context,
        new { access_token = "fake-token", session_id = sessionId, scenario = "cancel-before-commit" },
        context.RequestAborted);

    logger.LogInformation("[SC1] Response flushed session_id={SessionId}. Now wait cancelAfterMs={CancelAfterMs}ms",
        sessionId, cancelAfterMs);

    // 等一会儿再“取消/回滚”事务（commit/rollback 由中间件在 endpoint 返回后决定）
    await Task.Delay(cancelAfterMs, context.RequestAborted);
    context.Items[FakeUnitOfWorkMiddleware.ItemsKeyRollback] = true;
    logger.LogWarning("[SC1] Simulated rollback request for session_id={SessionId}", sessionId);

    // 保持请求继续运行一段时间，让“后续请求”发生在 commit 之前
    await Task.Delay(returnAfterMs - cancelAfterMs, context.RequestAborted);
    return;
});

// 场景 2：
// 1) 写出响应（客户端拿到 session_id 认为成功）
// 2) 仍保持请求“没结束”，中间件此时还没 commit
// 3) 下一请求立刻查库：找不到（因为还没 commit）
// 4) 等 endpoint 返回后才 commit
app.MapPost("/race/early-write", async (HttpContext context, int delayBeforeCommitMs = 1500) =>
{
    var logger = app.Logger;
    var tx = context.Items[FakeUnitOfWorkMiddleware.ItemsKeyTx] as FakeTransaction
             ?? throw new InvalidOperationException("Missing fake tx in Items.");

    delayBeforeCommitMs = Math.Max(1, delayBeforeCommitMs);

    var sessionId = Guid.NewGuid();
    tx.StageInsert(sessionId);

    logger.LogInformation("[SC2] Staged Insert session_id={SessionId} traceId={TraceId}", sessionId, context.TraceIdentifier);

    await WriteJsonAndFlush(context,
        new { access_token = "fake-token", session_id = sessionId, scenario = "early-write" },
        context.RequestAborted);

    logger.LogInformation("[SC2] Response flushed session_id={SessionId}. Now wait delayBeforeCommitMs={DelayBeforeCommitMs}ms",
        sessionId, delayBeforeCommitMs);

    await Task.Delay(delayBeforeCommitMs, context.RequestAborted);
    return;
});

app.Run();
