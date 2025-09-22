using SWLOR.Component.StatusEffect.Enums;
using SWLOR.Component.StatusEffect.Model;

namespace SWLOR.Component.StatusEffect.Contracts
{
    public interface IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects();
    }
}
