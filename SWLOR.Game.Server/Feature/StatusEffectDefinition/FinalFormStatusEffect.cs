using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FinalFormStatusEffect : StatusEffectBase
    {
        public override string Name => "Final Form";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public FinalFormStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 25;
            StatGroup.Stats[StatType.AttackDeflection] = 25;
        }

    }
}
