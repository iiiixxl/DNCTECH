using Authorization_Extend.Permissions.Definitions;
using Authorization_Extend.Permissions.Reports;

namespace Authorization_Extend.Permissions;

/// <summary>
/// 运行时新增/删除报表时，同步注册/移除动态权限定义（仿 ABP DynamicPermissionStore）。
/// </summary>
public interface IDynamicPermissionRegistrar
{
    void RegisterReportPermission(ReportRecord report);

    void UnregisterReportPermission(string reportCode);
}

public class DynamicPermissionRegistrar : IDynamicPermissionRegistrar
{
    private readonly IPermissionDefinitionManager _definitionManager;

    public DynamicPermissionRegistrar(IPermissionDefinitionManager definitionManager)
    {
        _definitionManager = definitionManager;
    }

    public void RegisterReportPermission(ReportRecord report)
    {
        var permissionName = PermissionNames.Reports.GetViewPermission(report.Code);
        if (_definitionManager.Exists(permissionName))
        {
            return;
        }

        _definitionManager.AddDynamic(new PermissionDefinition(
            permissionName,
            $"查看报表：{report.Name}",
            PermissionNames.Reports.Group + ".Views")
        {
            IsDynamic = true
        });
    }

    public void UnregisterReportPermission(string reportCode)
    {
        // 简化 Demo：动态权限只增不删定义；生产环境可扩展 RemoveDynamic
        _ = reportCode;
    }
}
