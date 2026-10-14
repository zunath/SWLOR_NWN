using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardiansResolveStatusEffect : StatusEffectBase
    {
        private int _remainingShield;

        public override string Name => "Guardian's Resolve";
        public override EffectIconType Icon => EffectIconType.GuardiansResolveStatusEffect;

        public GuardiansResolveStatusEffect()
        {
        }

        public GuardiansResolveStatusEffect(int shieldAmount)
        {
            _remainingShield = Math.Max(0, shieldAmount);
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            // The pool can shrink without damage, e.g. when a raised maximum HP trims it to the
            // engine's HP limit, so never track more shield than still exists.
            _remainingShield = Math.Min(
                _remainingShield,
                TemporaryHitPointEffects.GetRemainingIncludingPending(defender, TemporaryHitPointEffectKey.GuardiansResolve));
            if (_remainingShield <= 0)
            {
                StatusEffect.RemoveStatusEffect(defender, GetType(), Source);
                return;
            }

            if (damage <= 0)
                return;

            var absorbed = Math.Min(_remainingShield, damage);
            _remainingShield -= absorbed;

            var healing = Stat.ApplyHealingReceivedAdjustment(defender, GameMath.PercentOf(absorbed, 15));
            ApplyEffectToObject(DurationType.Instant, EffectHeal(healing), defender);

            if (_remainingShield <= 0)
            {
                StatusEffect.RemoveStatusEffect(defender, GetType(), Source);
            }
        }

        public override IStatusEffect Clone()
        {
            return new GuardiansResolveStatusEffect(_remainingShield);
        }
    }
}
