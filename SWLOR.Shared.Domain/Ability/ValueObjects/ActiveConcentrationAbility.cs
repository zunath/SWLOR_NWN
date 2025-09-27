using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Shared.Domain.Ability.ValueObjects
{
    public class ActiveConcentrationAbility
    {
        public ActiveConcentrationAbility(uint target, FeatType feat, StatusEffectType statusEffectType)
        {
            Target = target;
            Feat = feat;
            StatusEffectType = statusEffectType;
        }

        public uint Target { get; set; }
        public FeatType Feat { get; set; }
        public StatusEffectType StatusEffectType { get; set; }
    }
}
