namespace SWLOR.Game.Server.Domain.Abilities
{
    public interface IAbilityActivationRequirement
    {
        string CheckRequirements(uint player, AbilityDetail ability = null);
        void AfterActivationAction(uint player, AbilityDetail ability = null);
    }
}
