using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>One paid activation and result across captured weapon impacts.</summary>
    public interface ITwinPistolAbilityService
    {
        bool HasActiveActivation(uint actor);

        uint[] GetEligibleWeapons(uint actor);

        string Validate(uint actor, uint target, TwinPistolAbilitySpec spec);

        bool Begin(uint actor, uint target, TwinPistolAbilitySpec spec);

        void Complete(uint actor, TwinPistolAbilitySpec spec);

        void Cancel(uint actor, CombatRecordInterruptCause cause = CombatRecordInterruptCause.Other);

        /// <summary>
        /// A destroyed owner (a dismissed droid) never reaches its delayed completion, and its object ID may be
        /// reused; drop its cached activation and channel, and release the channel's deferred stamina-cost
        /// context, without touching the destroyed object.
        /// </summary>
        void ForgetDestroyed(uint actor);

        void InterruptChannel(uint actor);
    }
}
