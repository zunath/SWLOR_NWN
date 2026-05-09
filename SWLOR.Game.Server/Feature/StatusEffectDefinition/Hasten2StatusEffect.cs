using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Hasten2StatusEffect : StatusEffectBase
    {
        public override string Name => "Hasten II";
        public override EffectIconType Icon => EffectIconType.Haste;

        public Hasten2StatusEffect()
        {
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 20;
        }
    }
}
