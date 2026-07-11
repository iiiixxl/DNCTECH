namespace Authorization_Extend.TempPermissionAuthorization;

/// <summary>
/// 临时权限编码与策略名常量。
/// </summary>
public static class TempPermissionNames
{
    /// <summary>代审报销单（临时权限点）。</summary>
    public const string ExpenseApprove = "expense.approve";

    /// <summary>
    /// 策略名：要求持有未过期的 expense.approve 临时权限。
    /// 用原生 AddPolicy 登记，不替换 PolicyProvider。
    /// </summary>
    public const string ExpenseApprovePolicy = "Temp.ExpenseApprove";
}
