using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Authentication_jwt_cookie.DynamicClaims;

/// <summary>
/// 对应 ABP <c>AbpDynamicClaimsMiddleware</c>。
/// 本套 Demo 默认认证即 JWT，登录后用 session_id 再校验一次服务端会话。
/// </summary>
public class DynamicClaimsMiddleware
{
    private readonly RequestDelegate _next;

    public DynamicClaimsMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IOptions<DynamicClaimsOptions> options,
        IEnumerable<IDynamicClaimsPrincipalContributor> contributors)
    {
        if (!options.Value.IsEnabled || context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // 仅处理带 session_id 的身份
        if (!context.User.HasClaim(c => c.Type == AppClaimTypes.SessionId))
        {
            await _next(context);
            return;
        }

        var feature = context.Features.Get<IAuthenticateResultFeature>();
        var scheme = feature?.AuthenticateResult?.Ticket?.AuthenticationScheme
            ?? context.User.Identity.AuthenticationType
            ?? JwtBearerDefaults.AuthenticationScheme;

        var contributeContext = new DynamicClaimsContributeContext(context.User);
        foreach (var contributor in contributors)
        {
            await contributor.ContributeAsync(contributeContext);
        }

        context.User = contributeContext.Principal;

        if (context.User.Identity?.IsAuthenticated != true)
        {
            if (feature is not null)
            {
                feature.AuthenticateResult = AuthenticateResult.Fail("Session expired or revoked.");
            }
        }
        else if (feature is not null)
        {
            feature.AuthenticateResult = AuthenticateResult.Success(
                new AuthenticationTicket(
                    context.User,
                    feature.AuthenticateResult?.Properties,
                    scheme));
        }

        await _next(context);
    }
}
