namespace Authorization_Extend.FieldLevelAuthorization;

/// <summary>员工薪资数据源（演示用，真实项目换成 EF Core / 仓储即可）。</summary>
public interface IEmployeeStore
{
    Employee? Find(string userId);

    IReadOnlyList<Employee> All();
}
