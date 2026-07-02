namespace DynamicOption_Extend.DynamicOptions;

/// <summary>
/// 动态 Options 注册或使用不正确时抛出的异常。
/// </summary>
public class DynamicOptionsException : Exception
{
    public DynamicOptionsException(string message)
        : base(message)
    {
    }
}
