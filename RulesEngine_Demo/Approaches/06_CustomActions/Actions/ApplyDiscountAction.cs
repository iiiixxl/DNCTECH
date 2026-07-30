using RulesEngine.Actions;
using RulesEngine.Models;

namespace RulesEngine_Demo.Approaches.CustomActions.Actions;

/// <summary>
/// 自定义成功动作：根据 Context 中的 Factor / Subtract 产出可读折扣结果。
/// RulesEngine 6.x：<see cref="ActionBase.Run"/> 返回 <c>ValueTask&lt;object&gt;</c>。
/// </summary>
public sealed class ApplyDiscountAction : ActionBase
{
    public override ValueTask<object> Run(ActionContext context, RuleParameter[] ruleParameters)
    {
        if (TryReadDecimal(context, "Factor", out var factor))
            return new ValueTask<object>($"discount:{factor}");

        if (TryReadDecimal(context, "Subtract", out var subtract))
            return new ValueTask<object>($"subtract:{subtract}");

        return new ValueTask<object>("discount:none");
    }

    /// <summary>
    /// ActionContext 内部多以字符串保存 Context；兼容 decimal / double / string。
    /// </summary>
    private static bool TryReadDecimal(ActionContext context, string key, out decimal value)
    {
        value = 0m;

        if (context.TryGetContext<decimal>(key, out var d))
        {
            value = d;
            return true;
        }

        if (context.TryGetContext<double>(key, out var dbl))
        {
            value = (decimal)dbl;
            return true;
        }

        if (context.TryGetContext<int>(key, out var i))
        {
            value = i;
            return true;
        }

        if (context.TryGetContext<string>(key, out var s)
            && decimal.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }
}
