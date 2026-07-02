namespace Authorization_Extend.Permissions.Reports;

/// <summary>
/// 模拟 report_configs 表。
/// </summary>
public class InMemoryReportStore : IReportStore
{
    private readonly List<ReportRecord> _reports =
    [
        new() { Code = "SALES_DAILY", Name = "销售日报" },
        new() { Code = "STOCK_MONTHLY", Name = "库存月报" }
    ];

    public Task<IReadOnlyList<ReportRecord>> GetAllAsync()
    {
        lock (_reports)
        {
            return Task.FromResult<IReadOnlyList<ReportRecord>>(_reports.ToList());
        }
    }

    public Task<ReportRecord?> FindAsync(string code)
    {
        lock (_reports)
        {
            return Task.FromResult(_reports.FirstOrDefault(x =>
                x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task AddAsync(ReportRecord report)
    {
        lock (_reports)
        {
            if (_reports.Any(x => x.Code.Equals(report.Code, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"报表 {report.Code} 已存在");
            }

            _reports.Add(report);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string code)
    {
        lock (_reports)
        {
            _reports.RemoveAll(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        }

        return Task.CompletedTask;
    }
}
