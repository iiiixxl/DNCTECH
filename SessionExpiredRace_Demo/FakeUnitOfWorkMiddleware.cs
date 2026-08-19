using Microsoft.AspNetCore.Http;

namespace SessionExpiredRace_Demo;

public sealed class FakeUnitOfWorkMiddleware
{
    public const string ItemsKeyTx = "__fake_uow_tx";
    public const string ItemsKeyRollback = "__fake_uow_rollback";

    private readonly RequestDelegate _next;
    private readonly ILogger<FakeUnitOfWorkMiddleware> _logger;

    public FakeUnitOfWorkMiddleware(RequestDelegate next, ILogger<FakeUnitOfWorkMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var traceId = context.TraceIdentifier;
        var tx = new FakeTransaction();
        context.Items[ItemsKeyTx] = tx;

        _logger.LogInformation("[UOW] BEGIN traceId={TraceId} path={Path}", traceId, context.Request.Path);

        try
        {
            await _next(context);
        }
        finally
        {
            // 这里用“请求结束（endpoint 返回）”来模拟 ABP 的 CompleteAsync：
            // 客户端可能已经收到了响应，但 commit/rollback 仍要等到管道后续代码跑完。
            var rollback = false;
            if (context.Items.TryGetValue(ItemsKeyRollback, out var rb) && rb is bool b)
            {
                rollback = b;
            }

            if (rollback)
            {
                tx.Rollback();
                _logger.LogWarning("[UOW] ROLLBACK traceId={TraceId}", traceId);
            }
            else
            {
                tx.Commit();
                _logger.LogInformation("[UOW] COMMIT traceId={TraceId}", traceId);
            }
        }
    }
}

