using Authorization_Extend.Permissions.Definitions;

namespace Authorization_Extend.Permissions.Providers;

/// <summary>
/// 静态权限：编译时在代码里声明，类似 ABP 各模块的 PermissionDefinitionProvider。
/// </summary>
public class BookPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(PermissionDefinitionContext context)
    {
        var group = context.AddGroup(PermissionNames.Books.Group, "图书管理");

        group.AddPermission(PermissionNames.Books.Default, "查看图书");
        group.AddPermission(PermissionNames.Books.Create, "创建图书");
        group.AddPermission(PermissionNames.Books.Update, "编辑图书");
        group.AddPermission(PermissionNames.Books.Delete, "删除图书");
    }
}
