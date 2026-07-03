using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EnmityToStatusSourceStatusEffect : StatusEffectBase
    {
        private readonly int _enmityPercentAdjustment;

        public override string Name => "Covering Claws";
        public override EffectIconType Icon => EffectIconType.CoveringClawsStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;

        public EnmityToStatusSourceStatusEffect()
            : this(25)
        {
        }

        public EnmityToStatusSourceStatusEffect(int enmityPercentAdjustment)
        {
            _enmityPercentAdjustment = enmityPercentAdjustment;
            StatGroup.Stats[StatType.EnmityToStatusSourcePercentAdjustment] = _enmityPercentAdjustment;
        }

        public override IStatusEffect Clone()
        {
            return new EnmityToStatusSourceStatusEffect(_enmityPercentAdjustment);
        }
    }
}
