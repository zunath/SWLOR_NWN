using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ReloadTempoStatusEffect : StatusEffectBase
    {
        private readonly int _hastePercent;
        private readonly LimitedAttackCounter _attackCounter;

        public override string Name => "Reload Tempo";
        public override EffectIconType Icon => EffectIconType.ReloadTempoStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public ReloadTempoStatusEffect()
            : this(20, 2)
        {
        }

        public ReloadTempoStatusEffect(int hastePercent, int attackCount)
        {
            _hastePercent = hastePercent;
            _attackCounter = new LimitedAttackCounter(attackCount);
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = _hastePercent;
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
            {
                return;
            }

            if (!_attackCounter.TryConsume(Ability.GetActiveAbilityImpactSummary(attacker)))
                return;

            if (_attackCounter.RemainingAttacks <= 0)
                IsFlaggedForRemoval = true;
        }

        public override IStatusEffect Clone()
        {
            return new ReloadTempoStatusEffect(_hastePercent, _attackCounter.RemainingAttacks);
        }
    }
}
