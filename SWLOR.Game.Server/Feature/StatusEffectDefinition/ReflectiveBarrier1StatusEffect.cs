using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ReflectiveBarrier1StatusEffect : StatusEffectBase
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

        protected override void OnDamageTaken(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType)
        {
            // Damage notifications run before the engine applies the current hit. The hit that
            // consumes the final temporary HP therefore still sees an active pool, while the next
            // hit removes an exhausted barrier before the shared reflection stage reads its stats.
            RemoveWhenGuardianWardPoolEnds(defender);
        }

        private void RemoveWhenGuardianWardPoolEnds(uint creature)
        {
            if (TemporaryHitPointEffects.IsActivePoolFromSource(creature, "GUARDIAN_WARD", Source))
                return;

            StatusEffect.RemoveStatusEffect(creature, GetType(), Source, false);
        }
    }
}
