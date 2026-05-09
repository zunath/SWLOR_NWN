using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Hasten1StatusEffect : StatusEffectBase
    {
        public override string Name => "Hasten I";
        public override EffectIconType Icon => EffectIconType.Haste;

        public Hasten1StatusEffect()
        {
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 10;
        }
    }
}
