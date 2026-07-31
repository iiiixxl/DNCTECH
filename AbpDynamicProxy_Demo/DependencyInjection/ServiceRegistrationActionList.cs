namespace AbpDynamicProxy_Demo.DependencyInjection;

/// <summary>
/// 对应 ABP: ServiceRegistrationActionList + services.OnRegistered(...)
/// </summary>
public class ServiceRegistrationActionList : List<Action<OnServiceRegistredContext>>
{
}
