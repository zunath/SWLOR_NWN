namespace SWLOR.Shared.Domain.Contracts
{
    public interface IAbilityActivationRequirement
    {
        string CheckRequirements(uint player);
        void AfterActivationAction(uint player);
    }
}
