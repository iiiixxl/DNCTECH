namespace Authorization_Extend.Permissions;

/// <summary>
/// 权限名常量，避免魔法字符串。
/// </summary>
public static class PermissionNames
{
    public static class Books
    {
        public const string Group = "Books";

        public const string Default = Group;
        public const string Create = Group + ".Create";
        public const string Update = Group + ".Update";
        public const string Delete = Group + ".Delete";
    }

    public static class Reports
    {
        public const string Group = "Reports";
        public const string ManagementGroup = Group + ".Management";

        public const string Create = ManagementGroup + ".Create";
        public const string Delete = ManagementGroup + ".Delete";

        public const string ViewPrefix = Group + ".View.";

        /// <summary>根据报表 Code 生成动态查看权限名，如 Reports.View.SALES_DAILY。</summary>
        public static string GetViewPermission(string reportCode) => ViewPrefix + reportCode;
    }
}
