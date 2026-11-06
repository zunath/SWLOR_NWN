using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// What a rider perk of a performance requires. A part of the Performance service; <c>Service.Performance</c> forwards to it.
    /// </summary>
    public interface IPerformanceRiderService
    {
        /// <summary>The Performance skill rank that unlocks a perk's first level, or 0 when it has no rank requirement.</summary>
        int GetRequiredRank(PerkType perk);
    }
}
