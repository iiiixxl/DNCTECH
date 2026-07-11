namespace Authorization_Extend.ClaimsTransformation;

/// <summary>
/// 基于「转换后声明」的策略名常量。用原生 AddPolicy 登记，
/// 不替换 PolicyProvider，与其它动态权限方案互不冲突。
/// </summary>
public static class ClaimsPermissionPolicyNames
{
    /// <summary>要求拥有 finance.approve 权限声明（财务审批）。</summary>
    public const string FinanceApprove = "Claims.FinanceApprove";
}
