namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 本模块用到的自定义 Claim 类型常量。tenant_id 在登录（认证阶段）时写入。
/// </summary>
public static class ResourceClaimTypes
{
    public const string TenantId = "tenant_id";
}
