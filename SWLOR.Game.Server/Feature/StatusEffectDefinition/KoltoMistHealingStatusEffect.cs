using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class KoltoMistHealingStatusEffect : StatusEffectBase
    {
        private readonly float _totalPercent;
        private readonly int _tickCount;

        public override string Name => "Kolto Mist";
        public override EffectIconType Icon => EffectIconType.Regenerate;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 3f;
        public override bool PersistsOnLogout => false;

        public KoltoMistHealingStatusEffect()
        {
            _totalPercent = 0f;
            _tickCount = 1;
        }

        public KoltoMistHealingStatusEffect(float totalPercent, int tickCount)
        {
            _totalPercent = totalPercent;
            _tickCount = tickCount <= 0 ? 1 : tickCount;
        }

        protected override void Tick(uint creature)
        {
            if (_totalPercent <= 0f)
                return;

            FirstAidTreatmentAdjustments.ApplyMedicalScaledHeal(Source, creature, _totalPercent / _tickCount);
        }
    }
}
