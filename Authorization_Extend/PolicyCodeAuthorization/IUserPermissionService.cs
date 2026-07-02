namespace Authorization_Extend.PolicyCodeAuthorization;

/// <summary>
/// 模拟数据库：按用户 ID 查询是否拥有某权限编码。
/// </summary>
public interface IUserPermissionService
{
    Task<bool> UserHasPermissionAsync(string userId, string permissionCode);

    Task<IReadOnlyList<string>> GetUserPermissionsAsync(string userId);
}
