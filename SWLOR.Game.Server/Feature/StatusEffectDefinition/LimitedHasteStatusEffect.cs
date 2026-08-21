using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    [StatConfiguredIcon]
    public sealed class LimitedHasteStatusEffect : StatusEffectBase
    {
        private readonly int _hastePercent;
        private readonly LimitedAttackCounter _attackCounter;

        public override string Name => "Limited Haste";
        public override EffectIconType Icon { get; }
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public LimitedHasteStatusEffect()
            : this(0, EffectIconType.Invalid, new LimitedAttackCounter(0))
        {
        }

        public LimitedHasteStatusEffect(
            int hastePercent,
            int attackCount,
            EffectIconType icon,
            AbilityImpactSummary triggeringAbilityImpact)
            : this(
                hastePercent,
                icon,
                new LimitedAttackCounter(
                    attackCount,
                    triggeringAbilityImpact,
                    triggeringAbilityImpact == null))
        {
        }

        private LimitedHasteStatusEffect(
            int hastePercent,
            EffectIconType icon,
            LimitedAttackCounter attackCounter)
        {
            _hastePercent = hastePercent;
            _attackCounter = attackCounter;
            Icon = icon;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = hastePercent;
        }

        public override string CanApply(uint creature)
        {
            return Icon == EffectIconType.Invalid
                ? "Limited Haste requires a configured status icon."
                : string.Empty;
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

            if (damage <= 0)
                return;

            if (!_attackCounter.TryConsume(Ability.GetActiveAbilityImpactSummary(attacker)))
                return;

            if (_attackCounter.RemainingAttacks <= 0)
                IsFlaggedForRemoval = true;
        }

        public override IStatusEffect Clone()
        {
            return new LimitedHasteStatusEffect(_hastePercent, Icon, _attackCounter.Clone());
        }
    }
}
