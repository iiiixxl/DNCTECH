using System.Collections.Concurrent;
using System.Text.Json;
using RulesEngine.Actions;
using RulesEngine.Models;

namespace RulesEngine_Demo.Approaches.CustomActions.Actions;

/// <summary>
/// 演示用审计动作：把消息写入静态 <see cref="AuditSink"/>（生产应对接日志/消息队列）。
/// </summary>
public sealed class SendAuditLogAction : ActionBase
{
    public static ConcurrentQueue<string> AuditSink { get; } = new();

    public override ValueTask<object> Run(ActionContext context, RuleParameter[] ruleParameters)
    {
        var message = ReadMessage(context);
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        AuditSink.Enqueue(line);
        return new ValueTask<object>(line);
    }

    private static string ReadMessage(ActionContext context)
    {
        if (context.TryGetContext<string>("Message", out var msg) && !string.IsNullOrWhiteSpace(msg))
            return msg;

        if (context.TryGetContext<JsonElement>("Message", out var el))
        {
            if (el.ValueKind == JsonValueKind.String)
                return el.GetString() ?? "audit:empty";
            return el.ToString();
        }

        if (context.TryGetContext<object>("Message", out var raw) && raw is not null)
            return raw.ToString() ?? "audit:unnamed";

        return "audit:unnamed";
    }

    public static void Clear()
    {
        while (AuditSink.TryDequeue(out _)) { }
    }
}
