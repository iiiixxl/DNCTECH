using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Authorization_Extend.TempPermissionAuthorization;

/// <summary>
/// 串联多个 IClaimsTransformation：ASP.NET Core 默认只取「最后一个」注册的转换器，
/// 本类把声明转换篇 + 临时权限篇的转换器按顺序都跑一遍，避免互相覆盖。
/// </summary>
public class CompositeClaimsTransformation : IClaimsTransformation
{
    private readonly IEnumerable<IClaimsTransformation> _transformers;

    public CompositeClaimsTransformation(IEnumerable<IClaimsTransformation> transformers)
    {
        // 排除自己，防止 DI 扫描时把自己又塞进来造成递归
        _transformers = transformers
            .Where(t => t is not CompositeClaimsTransformation)
            .ToList();
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        foreach (var transformer in _transformers)
        {
            principal = await transformer.TransformAsync(principal);
        }

        return principal;
    }
}
