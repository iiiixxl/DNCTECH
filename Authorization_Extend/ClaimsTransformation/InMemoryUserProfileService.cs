namespace Authorization_Extend.ClaimsTransformation;

/// <summary>
/// 内存画像库。演示「同是管理员，职责却不同」：
/// - user-admin  → 财务部，细分角色 FinanceAdmin，拥有财务相关权限
/// - user-normal → 内容部，细分角色 ContentEditor，拥有内容编辑权限
/// 这些信息原始登录 Token 里都没有，全靠声明转换在每次请求时补进去。
/// </summary>
public class InMemoryUserProfileService : IUserProfileService
{
    private readonly Dictionary<string, UserProfile> _profiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["user-admin"] = new UserProfile
        {
            Department = "Finance",
            Roles = ["FinanceAdmin"],
            Permissions = ["finance.report.view", "finance.approve"]
        },
        ["user-normal"] = new UserProfile
        {
            Department = "Content",
            Roles = ["ContentEditor"],
            Permissions = ["content.article.edit"]
        }
    };

    public Task<UserProfile?> GetProfileAsync(string userId)
    {
        _profiles.TryGetValue(userId, out var profile);
        return Task.FromResult(profile);
    }
}
