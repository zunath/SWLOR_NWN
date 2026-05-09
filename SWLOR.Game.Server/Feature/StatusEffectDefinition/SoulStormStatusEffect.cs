using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoulStormStatusEffect : StatusEffectBase
    {
        public override string Name => "Soul Storm";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public SoulStormStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 35;
        }

    }
}
