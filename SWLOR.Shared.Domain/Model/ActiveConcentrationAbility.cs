using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Model
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
