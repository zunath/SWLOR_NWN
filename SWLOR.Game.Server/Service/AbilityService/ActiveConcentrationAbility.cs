using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public class ActiveConcentrationAbility
    {
        public ActiveConcentrationAbility(FeatType feat, StatusEffectType statusEffectType)
        {
            Feat = feat;
            StatusEffectType = statusEffectType;
        }

        public FeatType Feat { get; set; }
        public StatusEffectType StatusEffectType { get; set; }
    }
}
