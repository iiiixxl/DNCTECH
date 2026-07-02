using Authorization_Extend.Permissions.Definitions;
using Authorization_Extend.Permissions.Reports;

namespace Authorization_Extend.Permissions.Providers;

/// <summary>
/// 动态权限：启动时从存储加载报表，每条报表注册一个 View 权限。
/// </summary>
public class ReportPermissionDefinitionProvider : PermissionDefinitionProvider
{
    private readonly IReportStore _reportStore;

    public ReportPermissionDefinitionProvider(IReportStore reportStore)
    {
        _reportStore = reportStore;
    }

    public override void Define(PermissionDefinitionContext context)
    {
        var management = context.AddGroup(PermissionNames.Reports.ManagementGroup, "报表管理");
        management.AddPermission(PermissionNames.Reports.Create, "新增报表配置");
        management.AddPermission(PermissionNames.Reports.Delete, "删除报表配置");

        var views = context.AddGroup(PermissionNames.Reports.Group + ".Views", "报表查看");

        var reports = _reportStore.GetAllAsync().GetAwaiter().GetResult();
        foreach (var report in reports)
        {
            views.AddPermission(
                PermissionNames.Reports.GetViewPermission(report.Code),
                $"查看报表：{report.Name}");
        }
    }
}
