using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity2FA_Demo.Data;

/// <summary>
/// IdentityDbContext 会带上 AspNetUsers / AspNetRoles / AspNetUserTokens 等表。
/// 2FA 的 AuthenticatorKey、RecoveryCodes 默认存在 AspNetUserTokens。
/// </summary>
public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
