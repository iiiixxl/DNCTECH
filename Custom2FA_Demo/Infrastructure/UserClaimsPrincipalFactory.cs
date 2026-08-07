using System.Security.Claims;
using Custom2FA_Demo.Abstractions;
using Custom2FA_Demo.Domain;

namespace Custom2FA_Demo.Infrastructure;

/// <summary>对应 UserClaimsPrincipalFactory。</summary>
public sealed class UserClaimsPrincipalFactory : IUserClaimsPrincipalFactory
{
    public ClaimsPrincipal Create(AppUser user, string authenticationType, IEnumerable<Claim>? extra = null)
    {
        var identity = new ClaimsIdentity(authenticationType);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
        if (extra != null)
        {
            foreach (var c in extra)
                identity.AddClaim(c);
        }
        return new ClaimsPrincipal(identity);
    }
}
