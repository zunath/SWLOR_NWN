using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What the character services tell the combat recorder on top of <see cref="ICombatRecording"/>: the stat triggers and
    /// status effects only Character's types can name. Character owns the interface and the recorder implements it (supply
    /// rule 3 of MaintainabilityRefactorDomainOrder.md).
    /// </summary>
    public interface ICharacterRecording : ICombatRecording
    {
        CombatRecordAttributionScope BeginStatusEffectAttribution(uint source, uint creature, IStatusEffect statusEffect);

        CombatRecordAttributionScope BeginStatTriggerAttribution(uint creature, StatType stat);

        void RecordStatusApplied(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            bool isPermanent,
            bool isRefresh);

        void RecordStatusRemoved(uint creature, IStatusEffect statusEffect, bool isReplacement);
    }
}
