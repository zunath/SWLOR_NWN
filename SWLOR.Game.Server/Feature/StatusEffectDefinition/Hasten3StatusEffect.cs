using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Hasten3StatusEffect : StatusEffectBase
    {
        public override string Name => "Hasten III";
        public override EffectIconType Icon => EffectIconType.Haste;

        public Hasten3StatusEffect()
        {
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 30;
        }
    }
}
