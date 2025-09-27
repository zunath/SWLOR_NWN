using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;

namespace SWLOR.Component.StatusEffect.Contracts
{
    public interface IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects();
    }
}
