using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Tracks the Twin Pistol capstone mastery trials inside the trial area. The logic lives in
    /// <see cref="ITwinPistolMasteryService"/>; every member forwards to the container's instance.
    /// </summary>
    public static class TwinPistolMastery
    {
        private static ITwinPistolMasteryService Instance => FacadeAccessor.Resolve<ITwinPistolMasteryService>();

        public static void RecordOrdinaryHit(uint actor, uint target, bool companion) =>
            Instance.RecordOrdinaryHit(actor, target, companion);

        public static void RecordAbility(uint actor, bool anyHit, PerkType perk, int cadenceSpent) =>
            Instance.RecordAbility(actor, anyHit, perk, cadenceSpent);

        public static void ClearTrialState(uint actor) => Instance.ClearTrialState(actor);

        public static void RecordTargetChange(uint actor, uint target) => Instance.RecordTargetChange(actor, target);
    }
}
