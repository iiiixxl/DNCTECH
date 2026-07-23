namespace Refit_Demo.RefitDemo.Options;

public class RefitDemoOptions
{
    public const string SectionName = "RefitDemo";

    /// <summary>出站 BaseAddress，须与本进程监听地址一致（默认 http 配置文件）。</summary>
    public string BaseAddress { get; set; } = "http://127.0.0.1:5235";

    public int TimeoutSeconds { get; set; } = 30;
}
