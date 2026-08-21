using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DeadMansHandStatusEffect : StatusEffectBase
    {
        private const int CriticalRatePercent = 20;
        private readonly LimitedAttackCounter _attackCounter;

        public override string Name => "Dead Man's Hand";
        public override EffectIconType Icon => EffectIconType.DeadMansHandStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public DeadMansHandStatusEffect()
            : this(3)
        {
        }

        private DeadMansHandStatusEffect(int remainingAttacks)
        {
            _attackCounter = new LimitedAttackCounter(remainingAttacks);
            StatGroup.Stats[StatType.RangedCriticalRatePercentAdjustment] = CriticalRatePercent;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            var skillType = Combat.GetEquippedWeaponSkillType(creature);
            if (!Combat.IsRangedWeaponSkill(skillType))
                return;

            Combat.GrantNextAutoAttackNoDelay(
                creature,
                skillType,
                Math.Max(1, (int)Math.Ceiling(GetDurationSeconds(durationTicks))));
        }

        protected override void OnDamageDealt(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType != CombatDamageDeliveryType.Direct)
                return;

            if (damage <= 0 ||
                !Combat.IsRangedWeaponSkill(Combat.GetEquippedWeaponSkillType(attacker)))
            {
                return;
            }

            if (!_attackCounter.TryConsume(Ability.GetActiveAbilityImpactSummary(attacker)))
                return;

            if (_attackCounter.RemainingAttacks <= 0)
            {
                IsFlaggedForRemoval = true;
                return;
            }

            Combat.GrantNextAutoAttackNoDelay(
                attacker,
                Combat.GetEquippedWeaponSkillType(attacker),
                Math.Max(1, (int)Math.Ceiling(GetDurationSeconds(DurationTicks))));
        }

        public override IStatusEffect Clone()
        {
            return new DeadMansHandStatusEffect(_attackCounter.RemainingAttacks);
        }
    }
}
