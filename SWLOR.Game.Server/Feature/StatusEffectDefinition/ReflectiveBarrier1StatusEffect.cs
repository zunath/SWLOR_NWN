using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ReflectiveBarrier1StatusEffect : StatusEffectBase, IPreDamageStatusEffect
    {
        private const int BaseReflectionPercent = 8;

        public override string Name => "Reflective Barrier";
        public override EffectIconType Icon => EffectIconType.ReflectiveBarrier1StatusEffect;
        public override float Frequency => 1f;
        public override bool PersistsOnLogout => false;

        protected override void Apply(uint creature, int durationTicks)
        {
            var reflection = AbilityEffectScaling.ScaleDirectEffect(
                BaseReflectionPercent,
                GetAbilityScore(Source, AbilityType.Willpower),
                source: Source);

            StatGroup.Stats[StatType.ForceDamageReflectionPercentAdjustment] = reflection;
            StatGroup.Stats[StatType.ElementalDamageReflectionPercentAdjustment] = reflection;
        }

        protected override void Tick(uint creature)
        {
            RemoveWhenGuardianWardPoolEnds(creature);
        }

        public void OnBeforeDamageTaken(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            // Validate before the originating hit so a hit that consumes the final temporary HP
            // still reflects, while the next hit cannot read stale reflection stats.
            RemoveWhenGuardianWardPoolEnds(defender);
        }

        private void RemoveWhenGuardianWardPoolEnds(uint creature)
        {
            if (TemporaryHitPointEffects.IsActivePoolFromSource(
                    creature,
                    TemporaryHitPointEffectKey.GuardianWard,
                    Source))
                return;

            StatusEffect.RemoveStatusEffect(creature, GetType(), Source, false);
        }
    }
}
