namespace RulesEngine_Demo.Infrastructure;

/// <summary>
/// 暴露给规则表达式的数学助手。RulesEngine 通过 ReSettings.CustomTypes 注册后，
/// 表达式可写 RuleMath.Ceiling(x)、RuleMath.Max(a,b) 等。
/// </summary>
public static class RuleMath
{
    public static double Ceiling(double value) => Math.Ceiling(value);
    public static double Floor(double value) => Math.Floor(value);
    public static double Max(double a, double b) => Math.Max(a, b);
    public static double Min(double a, double b) => Math.Min(a, b);
    public static double Round(double value, int digits) => Math.Round(value, digits);
}
