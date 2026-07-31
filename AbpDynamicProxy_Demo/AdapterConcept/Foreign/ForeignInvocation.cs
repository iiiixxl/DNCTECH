namespace AbpDynamicProxy_Demo.AdapterConcept.Foreign;

/// <summary>
/// 模拟「第三方库」（类似 Castle）提供的一次方法调用上下文。
/// 名字丑、API 怪，是故意的——业务不想直接依赖它。
/// </summary>
public class ForeignInvocation
{
    public required string MethodName { get; init; }

    public required object[] Args { get; init; }

    /// <summary>
    /// 第三方继续往下执行的方式：同步 Action，没有 async。
    /// </summary>
    public required Action Continue { get; init; }

    public object? Result { get; set; }
}
