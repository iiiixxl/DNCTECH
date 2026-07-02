namespace Authorization_Extend.Permissions.Reports;

public interface IReportStore
{
    Task<IReadOnlyList<ReportRecord>> GetAllAsync();

    Task<ReportRecord?> FindAsync(string code);

    Task AddAsync(ReportRecord report);

    Task RemoveAsync(string code);
}
