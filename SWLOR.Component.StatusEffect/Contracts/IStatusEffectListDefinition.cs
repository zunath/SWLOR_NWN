using SWLOR.Component.StatusEffect.Model;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.StatusEffect.Contracts
{
    public interface IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects();
    }
}
