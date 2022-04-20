namespace SWLOR.Game.Server.Service.PerkService
{
    public interface IPerkRequirement
    {
        string CheckRequirements(uint player);
        string RequirementText { get; }
    }

}
