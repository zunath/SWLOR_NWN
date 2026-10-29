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
    }
}
