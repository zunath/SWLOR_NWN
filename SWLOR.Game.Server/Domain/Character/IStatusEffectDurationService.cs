using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Status effect durations (part 4 of the status effect split): the timing of an application, the refresh in place, the
    /// restart, the extension and the refresh of an active effect.
    /// </summary>
    public interface IStatusEffectDurationService
    {
        /// <summary>
        /// Works out how long an application lasts (the outgoing adjustments, resistance, immunity and the control budget). False,
        /// with the "resisted" message sent to the source, when nothing is left of the duration.
        /// </summary>
        bool ResolveApplicationTiming(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            float durationSeconds,
            bool isPermanent,
            ResistanceType resistanceOverride,
            CombatDamageType sourceDamageType,
            out StatusApplicationTiming timing);

        /// <summary>Refreshes an active effect of the same type in place when applying it again would produce the same state; false when the effect must be applied fresh.</summary>
        bool TryRefreshInPlace(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            int durationTicks,
            int durationTicksWithoutResistance,
            float durationSeconds,
            ResistanceType resistanceType);

        /// <summary>
        /// Restarts an active effect's duration exactly as re-applying the same effect from the same
        /// source would, without replacing it or repeating its application message. For effects whose
        /// re-application changes nothing else, such as Embattled with unchanged stacks.
        /// </summary>
        bool RestartStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds);

        bool ExtendStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds);

        bool RefreshStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid,
            CombatDamageType sourceDamageType = CombatDamageType.Invalid);
    }
}
