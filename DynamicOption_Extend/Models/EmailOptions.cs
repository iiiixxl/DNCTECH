namespace DynamicOption_Extend.Models;

/// <summary>
/// 邮件 SMTP 配置项，静态基线来自 appsettings.json，运行时由设置存储覆盖。
/// </summary>
public class EmailOptions
{
    public string SmtpHost { get; set; } = "localhost";

    public int SmtpPort { get; set; } = 25;

    public string SenderAddress { get; set; } = "noreply@localhost";

    public bool EnableSsl { get; set; }
}
