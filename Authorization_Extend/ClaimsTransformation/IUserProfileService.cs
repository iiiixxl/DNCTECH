namespace Authorization_Extend.ClaimsTransformation;

/// <summary>
/// 画像数据源。真实项目里对接数据库 / LDAP / 企业微信等，这里用内存模拟。
/// </summary>
public interface IUserProfileService
{
    Task<UserProfile?> GetProfileAsync(string userId);
}
