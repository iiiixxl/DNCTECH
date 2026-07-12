namespace Authorization_Extend.ResourceBasedAuthorization;

public interface IContractStore
{
    Contract? Find(int id);
}
