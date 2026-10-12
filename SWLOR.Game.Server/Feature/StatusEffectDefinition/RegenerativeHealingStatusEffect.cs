using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
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
        private readonly SkillType _supportSkill;
        private readonly ISet<uint> _supportCredited;

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
            _supportSkill = SkillType.Invalid;
            _supportCredited = new HashSet<uint>();
        }

        public RegenerativeHealingStatusEffect(
            string name,
            float totalPercent,
            int tickCount,
            bool appliesMedicalHealingBonus = false,
            SkillType supportSkill = SkillType.Invalid,
            ISet<uint> supportCredited = null)
        {
            _name = name;
            _totalPercent = totalPercent;
            _tickCount = tickCount <= 0 ? 1 : tickCount;
            _appliesMedicalHealingBonus = appliesMedicalHealingBonus;
            _supportSkill = supportSkill;
            _supportCredited = supportCredited ?? new HashSet<uint>();
        }

        protected override void Tick(uint creature)
        {
            if (_totalPercent <= 0f)
                return;

            if (_appliesMedicalHealingBonus)
                FirstAidTreatmentAdjustments.ApplyMedicalScaledHeal(Source, creature, _totalPercent / _tickCount);
            else
                AbilityEffectScaling.ApplyScaledHeal(Source, creature, _totalPercent / _tickCount);

            // Each tick credits enemies that engaged the patient after the cast, once per cast.
            if (_supportSkill != SkillType.Invalid)
                CombatPoint.AddSupportCombatPoint(Source, new[] { creature }, _supportSkill, alreadyCredited: _supportCredited);
        }
    }
}
