using AbpDynamicProxy_Demo.AdapterConcept.App;

namespace AbpDynamicProxy_Demo.AdapterConcept.Demo;

/// <summary>
/// 我们写的横切逻辑（类似 AuthorizationInterceptor）。
/// 注意：这里完全没有 Foreign* 类型 —— 这就是适配的目的。
/// </summary>
public class PermissionCheckInterceptor : IAppInterceptor
{
    private readonly HashSet<string> _grantedPermissions;
    private readonly List<string> _trace;

    public PermissionCheckInterceptor(IEnumerable<string> grantedPermissions, List<string> trace)
    {
        _grantedPermissions = grantedPermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _trace = trace;
    }

    public async Task InterceptAsync(IAppInvocation invocation)
    {
        // 演示：Create 需要 Demo.Users.Create
        var required = invocation.MethodName == "Create"
            ? "Demo.Users.Create"
            : "Demo.Users";

        _trace.Add($"我们的 PermissionCheckInterceptor：检查方法 {invocation.MethodName}，需要权限 {required}");

        if (!_grantedPermissions.Contains(required))
        {
            _trace.Add($"鉴权失败：缺少 {required}，不调用 ProceedAsync（业务不会执行）");
            throw new InvalidOperationException($"Forbidden: missing permission {required}");
        }

        _trace.Add("鉴权通过 → ProceedAsync → 进入真实业务（或下一个拦截器）");
        await invocation.ProceedAsync();
        _trace.Add($"业务已返回，ReturnValue = {invocation.ReturnValue}");
    }
}
