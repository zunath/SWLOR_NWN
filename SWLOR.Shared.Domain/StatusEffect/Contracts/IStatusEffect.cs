using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Shared.Domain.StatusEffect.Contracts
{
    public interface IStatusEffect
    {
        string Id { get; }
        uint Source { get; }
        StatusEffectActivationType ActivationType { get; }
        StatusEffectSourceType SourceType { get; }
        string Name { get; }
        EffectIconType Icon { get; }
        StatusEffectStackType StackingType { get; }
        bool IsFlaggedForRemoval { get; }
        bool SendsApplicationMessage { get; }
        bool SendsWornOffMessage { get; }
        bool IsRemovedOnJobChange { get; }
        float Frequency { get; }
        public StatGroup StatGroup { get; }
        public List<Type> MorePowerfulEffectTypes { get; }
        public List<Type> LessPowerfulEffectTypes { get; }
        string CanApply(uint creature);
        void ApplyEffect(uint source, uint creature, int durationTicks);
        void RemoveEffect(uint creature);
        void TickEffect(uint creature);
        void OnHitEffect(uint creature, uint target, int damage);
    }
}
