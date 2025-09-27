namespace SWLOR.Shared.Domain.Ability.Contracts
{
    public interface IAbilityActivationRequirement
    {
        string CheckRequirements(uint player);
        void AfterActivationAction(uint player);
    }
}
