namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 标记「属于某个租户」的业务资源。实现后共用 <see cref="SameTenantAuthorizationHandler"/>。
/// </summary>
public interface ITenantScoped
{
    string TenantId { get; }
}
