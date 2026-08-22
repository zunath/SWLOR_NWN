using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FinalFormStatusEffect : StatusEffectBase
    {
        public override string Name => "Final Form";
        public override EffectIconType Icon => EffectIconType.FinalFormStatusEffect;
        public FinalFormStatusEffect()
        {
            StatGroup.Stats[StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment] = 15;
            StatGroup.Stats[StatType.MeleeDeflection] = 15;
        }

    }
}
