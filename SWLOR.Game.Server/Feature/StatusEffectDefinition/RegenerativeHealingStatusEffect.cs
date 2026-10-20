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
        private readonly HealAmount _healPerTick;
        private readonly float _multiplier;
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
            _healPerTick = default;
            _multiplier = 1f;
            _appliesMedicalHealingBonus = false;
            _supportSkill = SkillType.Invalid;
            _supportCredited = new HashSet<uint>();
        }

        /// <param name="multiplier">Scales each tick, such as the caster's Force affinity when the regeneration was cast.</param>
        public RegenerativeHealingStatusEffect(
            string name,
            HealAmount healPerTick,
            float multiplier = 1f,
            bool appliesMedicalHealingBonus = false,
            SkillType supportSkill = SkillType.Invalid,
            ISet<uint> supportCredited = null)
        {
            _name = name;
            _healPerTick = healPerTick;
            _multiplier = multiplier;
            _appliesMedicalHealingBonus = appliesMedicalHealingBonus;
            _supportSkill = supportSkill;
            _supportCredited = supportCredited ?? new HashSet<uint>();
        }

        protected override void Tick(uint creature)
        {
            if (_healPerTick.Flat <= 0 && _healPerTick.PercentOfMaxHp <= 0f)
                return;

            if (_appliesMedicalHealingBonus)
                FirstAidTreatmentAdjustments.ApplyMedicalScaledHeal(Source, creature, _healPerTick, multiplier: _multiplier);
            else
                AbilityEffectScaling.ApplyScaledHeal(Source, creature, _healPerTick, multiplier: _multiplier);

            // Each tick credits enemies that engaged the patient after the cast, once per cast.
            if (_supportSkill != SkillType.Invalid)
                CombatPoint.AddSupportCombatPoint(Source, new[] { creature }, _supportSkill, alreadyCredited: _supportCredited);
        }
    }
}
