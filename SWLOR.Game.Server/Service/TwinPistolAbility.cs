using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// One paid activation and result across captured weapon impacts. The logic lives in
    /// <see cref="ITwinPistolAbilityService"/>; every member forwards to the container's instance.
    /// </summary>
    public static class TwinPistolAbility
    {
        private static ITwinPistolAbilityService Instance => FacadeAccessor.Resolve<ITwinPistolAbilityService>();

        public static bool HasActiveActivation(uint actor) =>
            Instance.HasActiveActivation(actor);

        public static uint[] GetEligibleWeapons(uint actor) =>
            Instance.GetEligibleWeapons(actor);

        public static string Validate(uint actor, uint target, TwinPistolAbilitySpec spec) =>
            Instance.Validate(actor, target, spec);

        public static bool Begin(uint actor, uint target, TwinPistolAbilitySpec spec) =>
            Instance.Begin(actor, target, spec);

        public static void Complete(uint actor, TwinPistolAbilitySpec spec) =>
            Instance.Complete(actor, spec);

        public static void Cancel(uint actor, CombatRecordInterruptCause cause = CombatRecordInterruptCause.Other) =>
            Instance.Cancel(actor, cause);

        /// <summary>
        /// A destroyed owner (a dismissed droid) never reaches its delayed completion, and its object ID may be
        /// reused; drop its cached activation and channel, and release the channel's deferred stamina-cost
        /// context, without touching the destroyed object.
        /// </summary>
        public static void ForgetDestroyed(uint actor) =>
            Instance.ForgetDestroyed(actor);

        public static void InterruptChannel(uint actor) =>
            Instance.InterruptChannel(actor);
    }
}
