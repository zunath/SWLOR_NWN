using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// What a rider perk of a performance requires: the lookup that used to be static in
    /// <c>PerformanceRiderCatalog.GetRequiredRank</c>, moved here unchanged. The pure perk tables stay in the catalog.
    /// </summary>
    public class PerformanceRiderService : IPerformanceRiderService
    {
        private readonly IPerkService _perk;

        public PerformanceRiderService(IPerkService perk)
        {
            _perk = perk;
        }

        public int GetRequiredRank(PerkType perk)
        {
            if (!TryGetPerkDetails(perk, out var detail) || !detail.PerkLevels.TryGetValue(1, out var level))
                return 0;

            return level.Requirements.OfType<PerkRequirementSkill>().Select(requirement => requirement.RequiredRank).FirstOrDefault();
        }

        public bool IsRoutineUnlocked(uint player, PerformanceRoutineType routine)
        {
            return GetPerkLevel(player, PerformanceRiderCatalog.GetUnlockPerk(routine)) > 0;
        }

        public bool IsSetUnlocked(uint player, PerformanceSetType set)
        {
            return GetPerkLevel(player, PerformanceRiderCatalog.GetUnlockPerk(set)) > 0;
        }

        public bool CanBlendSecondRoutine(uint player, PerformanceRoutineType routine)
        {
            return IsRoutineUnlocked(player, routine) && GetPerkLevel(player, PerkType.Showstopper) > 0;
        }

        public bool CanWeaveSecondSet(uint player, PerformanceSetType set)
        {
            return IsSetUnlocked(player, set) && GetPerkLevel(player, PerkType.Virtuoso) > 0;
        }

        protected virtual int GetPerkLevel(uint creature, PerkType perk) => _perk.GetPerkLevel(creature, perk);

        protected virtual bool TryGetPerkDetails(PerkType perk, out PerkDetail detail) => _perk.TryGetPerkDetails(perk, out detail);
    }
}
