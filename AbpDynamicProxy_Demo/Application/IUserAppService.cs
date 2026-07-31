namespace AbpDynamicProxy_Demo.Application;

public interface IUserAppService
{
    Task<IReadOnlyList<string>> GetListAsync();

    Task<string> CreateAsync(string userName);

    Task DeleteAsync(string userName);
}
