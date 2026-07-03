using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EnmityToStatusSourceStatusEffect : StatusEffectBase
    {
        private readonly string _name;
        private readonly EffectIconType _icon;
        private readonly int _enmityPercentAdjustment;

        public override string Name => _name;
        public override EffectIconType Icon => _icon;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;

        public EnmityToStatusSourceStatusEffect()
            : this(25, "Enmity To Status Source", EffectIconType.Invalid)
        {
        }

        public EnmityToStatusSourceStatusEffect(
            int enmityPercentAdjustment,
            string name = "Enmity To Status Source",
            EffectIconType icon = EffectIconType.Invalid)
        {
            _name = name;
            _icon = icon;
            _enmityPercentAdjustment = enmityPercentAdjustment;
            StatGroup.Stats[StatType.EnmityToStatusSourcePercentAdjustment] = _enmityPercentAdjustment;
        }

        public override IStatusEffect Clone()
        {
            return new EnmityToStatusSourceStatusEffect(_enmityPercentAdjustment, _name, _icon);
        }
    }
}
