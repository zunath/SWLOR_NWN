using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardiansResolveStatusEffect : StatusEffectBase, ITemporaryHitPointPoolStatusEffect
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

        public string TemporaryHitPointPoolKey => TemporaryHitPointEffectKey.GuardiansResolve;

        public void OnTemporaryHitPointPoolTrimmed(uint creature, int remaining)
        {
            _remainingShield = Math.Min(_remainingShield, Math.Max(0, remaining));
            if (_remainingShield <= 0)
                StatusEffect.RemoveStatusEffect(creature, GetType(), Source);
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            if (damage <= 0 || _remainingShield <= 0)
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
