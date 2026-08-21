using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    [StatConfiguredIcon]
    public sealed class LimitedHasteStatusEffect : StatusEffectBase,
        IAttackAttemptStatusEffect,
        ILimitedAttackDelayReductionStatusEffect
    {
        private readonly int _hastePercent;
        private readonly SkillType _skillType;
        private readonly LimitedAttackCounter _attackCounter;

        public override string Name => "Limited Haste";
        public override EffectIconType Icon { get; }
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public int AttackDelayReductionPercent => _hastePercent;
        public int RemainingAttacks => _attackCounter.RemainingAttacks;

        public LimitedHasteStatusEffect()
            : this(0, SkillType.Invalid, EffectIconType.Invalid, new LimitedAttackCounter(0))
        {
        }

        public LimitedHasteStatusEffect(
            int hastePercent,
            int attackCount,
            SkillType skillType,
            EffectIconType icon,
            AbilityImpactSummary triggeringAbilityImpact)
            : this(
                hastePercent,
                skillType,
                icon,
                new LimitedAttackCounter(
                    attackCount,
                    triggeringAbilityImpact))
        {
        }

        private LimitedHasteStatusEffect(
            int hastePercent,
            SkillType skillType,
            EffectIconType icon,
            LimitedAttackCounter attackCounter)
        {
            _hastePercent = hastePercent;
            _skillType = skillType;
            _attackCounter = attackCounter;
            Icon = icon;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = hastePercent;
        }

        public override string CanApply(uint creature)
        {
            return Icon == EffectIconType.Invalid
                ? "Limited Haste requires a configured status icon."
                : _skillType == SkillType.Invalid
                    ? "Limited Haste requires a configured attack skill."
                : string.Empty;
        }

        public bool AppliesToSkill(SkillType skillType)
        {
            return skillType == _skillType;
        }

        public void OnAttackAttemptedEffect(
            uint attacker,
            SkillType skillType,
            AbilityImpactSummary abilityImpact)
        {
            if (!AppliesToSkill(skillType))
                return;

            if (!_attackCounter.TryConsume(abilityImpact))
                return;

            if (_attackCounter.RemainingAttacks <= 0)
                IsFlaggedForRemoval = true;
        }

        public override IStatusEffect Clone()
        {
            return new LimitedHasteStatusEffect(_hastePercent, _skillType, Icon, _attackCounter.Clone());
        }
    }
}
