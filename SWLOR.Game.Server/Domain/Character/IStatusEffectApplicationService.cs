using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Applying status effects (part 3 of the status effect split): the checks, the replaced and conflicting effects, the
    /// activation and the messages. The trackers, the removal, the durations and the native effect stay in
    /// <see cref="IStatusEffectService"/> until their parts move.
    /// </summary>
    public interface IStatusEffectApplicationService
    {
        void ApplyPermanentStatusEffect<T>(uint source, uint creature)
            where T: IStatusEffect;

        void ApplyPermanentStatusEffect(Type type, uint source, uint creature);

        bool ApplyStatusEffect<T>(
            uint source,
            uint creature,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
            where T: IStatusEffect;

        bool ApplyStatusEffect<T>(
            uint source,
            uint creature,
            float durationSeconds,
            CombatDamageType sourceDamageType)
            where T : IStatusEffect;

        bool ApplyStatusEffect(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid);

        bool ApplyStatusEffect(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            CombatDamageType sourceDamageType);

        bool ApplyStatusEffect(
            uint source,
            uint creature,
            Type statusEffectClass,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid);

        bool ApplyStatusEffect(
            uint source,
            uint creature,
            Type statusEffectClass,
            float durationSeconds,
            CombatDamageType sourceDamageType,
            Type replacedStatusEffectType = null);
    }
}
