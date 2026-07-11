namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>
/// 内存模拟员工薪资表。
/// 与登录账号对应：user-admin / user-normal 各一条，方便对照「看自己」与「HR 看全部」。
/// </summary>
public class InMemoryEmployeeStore : IEmployeeStore
{
    private readonly List<Employee> _employees =
    [
        new Employee
        {
            UserId = "user-admin",
            Name = "王 HR",
            BaseSalary = 18000m,
            Bonus = 8000m,
            SocialSecurityDetail = "养老 8% / 医疗 2% / 公积金 12%"
        },
        new Employee
        {
            UserId = "user-normal",
            Name = "张三",
            BaseSalary = 12000m,
            Bonus = 3500m,
            SocialSecurityDetail = "养老 8% / 医疗 2% / 公积金 7%"
        }
    ];

    public Employee? Find(string userId) =>
        _employees.FirstOrDefault(e =>
            string.Equals(e.UserId, userId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Employee> All() => _employees;
}
