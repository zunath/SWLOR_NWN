using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.AbilityService
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
