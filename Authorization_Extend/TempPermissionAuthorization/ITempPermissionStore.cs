namespace Authorization_Extend.TempPermissionAuthorization;

/// <summary>
/// 临时授权存储：主管授权时写入，声明转换时按 userId 读取仍有效的记录。
/// </summary>
public interface ITempPermissionStore
{
    /// <summary>授予临时权限（同 userId + permission 覆盖为最新一条）。</summary>
    Task GrantAsync(TempPermissionGrant grant);

    /// <summary>查询某用户当前仍有效的临时授权。</summary>
    Task<IReadOnlyList<TempPermissionGrant>> GetActiveGrantsAsync(string userId);

    /// <summary>查询某用户全部授权记录（含已过期，便于对照演示）。</summary>
    Task<IReadOnlyList<TempPermissionGrant>> GetAllGrantsAsync(string userId);

    /// <summary>主动撤销某用户的某条临时权限。</summary>
    Task RevokeAsync(string userId, string permission);
}
