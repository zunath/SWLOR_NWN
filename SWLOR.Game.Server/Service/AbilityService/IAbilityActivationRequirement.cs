namespace SWLOR.Game.Server.Service.AbilityService
{
    public interface IAbilityActivationRequirement
    {
        string CheckRequirements(uint player);
        void AfterActivationAction(uint player);
    }
}
