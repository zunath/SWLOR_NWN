using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WhirlingGuardStatusEffect : StatusEffectBase
    {
        public override string Name => "Whirling Guard";
        public override EffectIconType Icon => EffectIconType.WhirlingGuardStatusEffect;

        public WhirlingGuardStatusEffect()
        {
            StatGroup.Stats[StatType.Guard] = 20;
            StatGroup.Stats[StatType.GuardRetaliationDMG] = 8;
        }
    }
}
