namespace SWLOR.Game.Server.Service.AbilityService
{
    public interface IAbilityActivationRequirement
    {
        string CheckRequirements(uint player, AbilityDetail ability = null);
        void AfterActivationAction(uint player, AbilityDetail ability = null);
    }
}
