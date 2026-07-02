using Microsoft.AspNetCore.Authorization;

namespace Authorization_Extend.SimpleNativeAuthorization;

/// <summary>
/// 简化原生授权：直接用 Roles 属性，无需在 Program 中逐个 AddPolicy。
/// </summary>
/// <remarks>
/// 等价于 [Authorize(Roles = "Admin,User")]，但支持多角色参数写法更清晰。
/// 适合「角色 = 权限」的简单场景，无法做运行时动态授权。
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRolesAttribute : AuthorizeAttribute
{
    public RequireRolesAttribute(params string[] roles)
    {
        Roles = string.Join(',', roles);
    }
}
