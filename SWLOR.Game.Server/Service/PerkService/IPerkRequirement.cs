using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PerkService
{
    public interface IPerkRequirement
    {
        string CheckRequirements(uint player);

        /// <summary>
        /// Checks the requirement against a player's record and active beast instead of the live character,
        /// for saved characters upgraded outside the engine. Returns the same message as the live check.
        /// </summary>
        string CheckRequirements(Player dbPlayer, Beast activeBeast);
        string RequirementText { get; }
        PerkRequirementCategory Category { get; }
    }

}
