namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 同时带租户与归属人的资源。需要「同租户 + 本人」双重校验的实体实现此接口即可，
/// 调用 <see cref="ResourceAuthorizationPolicyNames.OwnerInTenant"/> 策略，无需新 Handler。
/// </summary>
public interface ITenantOwnedResource : ITenantScoped, IOwnedResource
{
}
