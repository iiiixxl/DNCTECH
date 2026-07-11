namespace Authorization_Extend.ClaimsTransformation;

/// <summary>
/// 用户「细粒度画像」：登录 Token 里通常只有粗粒度的 role（如 Admin），
/// 真正的部门、细分角色、权限点往往存在业务库里。声明转换就是把这些补进当前用户的身份。
/// </summary>
public class UserProfile
{
    /// <summary>部门，用来区分「财务管理员」和「内容管理员」这类同 role 不同职责的身份。</summary>
    public string Department { get; init; } = string.Empty;

    /// <summary>细粒度角色（原始 Token 里没有，登录后从库里补）。</summary>
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>权限点声明。</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
