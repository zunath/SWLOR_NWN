namespace SWLOR.Game.Server.Domain.Character
{
    public interface IAbilityActivationRequirement
    {
        string CheckRequirements(uint player, AbilityDetail ability = null);
        void AfterActivationAction(uint player, AbilityDetail ability = null);
    }
}
