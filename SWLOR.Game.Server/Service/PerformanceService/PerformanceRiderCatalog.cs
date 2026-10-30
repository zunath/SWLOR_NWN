using System.Linq;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    public static class PerformanceRiderCatalog
    {
        public static PerkType GetUnlockPerk(PerformanceRoutineType routine)
        {
            return routine switch
            {
                PerformanceRoutineType.LightFeet => PerkType.LightFeet,
                PerformanceRoutineType.PowerStep => PerkType.PowerStep,
                PerformanceRoutineType.Quickstep => PerkType.Quickstep,
                PerformanceRoutineType.VigorReel => PerkType.VigorReel,
                _ => PerkType.Dance
            };
        }

        public static PerkType GetUnlockPerk(PerformanceSetType set)
        {
            return set switch
            {
                PerformanceSetType.SteadyHands => PerkType.SteadyHands,
                PerformanceSetType.KeenEar => PerkType.KeenEar,
                PerformanceSetType.AnglersLullaby => PerkType.AnglersLullaby,
                PerformanceSetType.StillWaters => PerkType.StillWaters,
                PerformanceSetType.SilverTongue => PerkType.SilverTongue,
                _ => PerkType.PlayInstrument
            };
        }

        /// <summary>The Performance skill rank that unlocks a perk's first level, or 0 when it has no rank requirement.</summary>
        public static int GetRequiredRank(PerkType perk)
        {
            if (!Perk.TryGetPerkDetails(perk, out var detail) || !detail.PerkLevels.TryGetValue(1, out var level))
                return 0;

            return level.Requirements.OfType<PerkRequirementSkill>().Select(requirement => requirement.RequiredRank).FirstOrDefault();
        }
    }
}
