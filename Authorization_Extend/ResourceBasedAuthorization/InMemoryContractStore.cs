namespace Authorization_Extend.ResourceBasedAuthorization;

public class InMemoryContractStore : IContractStore
{
    private readonly List<Contract> _contracts =
    [
        new Contract { Id = 1, TenantId = "tenant-a", OwnerUserId = "user-admin", Title = "A 租户-admin 的合同" },
        new Contract { Id = 2, TenantId = "tenant-b", OwnerUserId = "user-normal", Title = "B 租户-user 的合同" }
    ];

    public Contract? Find(int id) => _contracts.FirstOrDefault(c => c.Id == id);
}
