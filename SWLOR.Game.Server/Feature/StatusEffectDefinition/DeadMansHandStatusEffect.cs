using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DeadMansHandStatusEffect : StatusEffectBase,
        IAttackAttemptStatusEffect,
        ILimitedAttackNoDelayStatusEffect
    {
        private const int CriticalRatePercent = 20;
        private readonly LimitedAttackCounter _attackCounter;

        public override string Name => "Dead Man's Hand";
        public override EffectIconType Icon => EffectIconType.DeadMansHandStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public int RemainingAttacks => _attackCounter.RemainingAttacks;

        public DeadMansHandStatusEffect()
            : this(new LimitedAttackCounter(3))
        {
        }

        public DeadMansHandStatusEffect(AbilityImpactSummary triggeringAbilityImpact)
            : this(new LimitedAttackCounter(3, triggeringAbilityImpact))
        {
        }

        private DeadMansHandStatusEffect(LimitedAttackCounter attackCounter)
        {
            _attackCounter = attackCounter;
            StatGroup.Stats[StatType.RangedCriticalRatePercentAdjustment] = CriticalRatePercent;
            StatGroup.Stats[StatType.RangedAttackNoDelay] = 1;
        }

        public bool AppliesToSkill(SkillType skillType)
        {
            return Combat.IsRangedWeaponSkill(skillType);
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
            {
                IsFlaggedForRemoval = true;
            }
        }

        public override IStatusEffect Clone()
        {
            return new DeadMansHandStatusEffect(_attackCounter.Clone());
        }
    }
}
