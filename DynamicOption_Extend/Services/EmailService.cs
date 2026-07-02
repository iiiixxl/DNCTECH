using DynamicOption_Extend.Models;
using Microsoft.Extensions.Options;

namespace DynamicOption_Extend.Services;

/// <summary>
/// 业务服务只依赖 IOptions&lt;EmailOptions&gt;，无需感知动态来源。
/// </summary>
public class EmailService
{
    private readonly IOptions<EmailOptions> _options;

    public EmailService(IOptions<EmailOptions> options)
    {
        _options = options;
    }

    public void PrintCurrentConfig(string label)
    {
        var email = _options.Value;
        Console.WriteLine($"[{label}]");
        Console.WriteLine($"  SmtpHost       : {email.SmtpHost}");
        Console.WriteLine($"  SmtpPort       : {email.SmtpPort}");
        Console.WriteLine($"  SenderAddress  : {email.SenderAddress}");
        Console.WriteLine($"  EnableSsl      : {email.EnableSsl}");
        Console.WriteLine();
    }
}
