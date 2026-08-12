using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RestoredFPHasteStatusEffect : StatusEffectBase
    {
        private readonly int _haste;

        public override string Name => "Restored FP Haste";
        public override EffectIconType Icon => EffectIconType.RestoredFPHasteStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public RestoredFPHasteStatusEffect()
            : this(10)
        {
        }

        public RestoredFPHasteStatusEffect(int haste)
        {
            _haste = haste;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = haste;
        }

        public override IStatusEffect Clone()
        {
            return new RestoredFPHasteStatusEffect(_haste);
        }
    }
}
