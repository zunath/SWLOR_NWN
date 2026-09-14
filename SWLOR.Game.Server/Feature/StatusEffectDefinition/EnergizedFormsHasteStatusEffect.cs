using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EnergizedFormsHasteStatusEffect : StatusEffectBase
    {
        private readonly int _haste;

        public override string Name => "Energized Forms: Haste";
        public override EffectIconType Icon => EffectIconType.EnergizedFormsHasteStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public EnergizedFormsHasteStatusEffect()
            : this(10)
        {
        }

        public EnergizedFormsHasteStatusEffect(int haste)
        {
            _haste = haste;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = haste;
        }

        public override IStatusEffect Clone()
        {
            return new EnergizedFormsHasteStatusEffect(_haste);
        }
    }
}
