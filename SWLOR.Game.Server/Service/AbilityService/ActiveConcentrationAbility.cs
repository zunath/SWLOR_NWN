using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public class ActiveConcentrationAbility
    {
        public ActiveConcentrationAbility(Feat feat, StatusEffectType statusEffectType)
        {
            Feat = feat;
            StatusEffectType = statusEffectType;
        }

        public Feat Feat { get; set; }
        public StatusEffectType StatusEffectType { get; set; }
    }
}
