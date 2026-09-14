using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RegenerativeHealingStatusEffect : StatusEffectBase
    {
        private readonly string _name;
        private readonly float _totalPercent;
        private readonly int _tickCount;
        private readonly bool _appliesMedicalHealingBonus;

        public override string Name => _name;
        public override EffectIconType Icon => EffectIconType.RegenerativeHealingStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 3f;
        public override bool PersistsOnLogout => false;

        public RegenerativeHealingStatusEffect()
        {
            _name = "Regeneration";
            _totalPercent = 0f;
            _tickCount = 1;
            _appliesMedicalHealingBonus = false;
        }

        public RegenerativeHealingStatusEffect(
            string name,
            float totalPercent,
            int tickCount,
            bool appliesMedicalHealingBonus = false)
        {
            _name = name;
            _totalPercent = totalPercent;
            _tickCount = tickCount <= 0 ? 1 : tickCount;
            _appliesMedicalHealingBonus = appliesMedicalHealingBonus;
        }

        protected override void Tick(uint creature)
        {
            if (_totalPercent <= 0f)
                return;

            if (_appliesMedicalHealingBonus)
                FirstAidTreatmentAdjustments.ApplyMedicalScaledHeal(Source, creature, _totalPercent / _tickCount);
            else
                AbilityEffectScaling.ApplyScaledHeal(Source, creature, _totalPercent / _tickCount);
        }
    }
}
