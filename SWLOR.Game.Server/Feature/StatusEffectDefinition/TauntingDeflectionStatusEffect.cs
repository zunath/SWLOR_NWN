using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TauntingDeflectionStatusEffect : StatusEffectBase
    {
        public override string Name => "Taunting Deflection";
        public override EffectIconType Icon => EffectIconType.Taunted;
        public TauntingDeflectionStatusEffect()
        {
            StatGroup.Stats[StatType.AttackDeflection] = 10;
        }

    }
}
