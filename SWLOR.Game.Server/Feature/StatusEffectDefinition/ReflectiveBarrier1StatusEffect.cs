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
            RemoveWhenGuardianWardPoolEnds(creature, delayUntilAfterDamageResolution: false);
        }

        protected override void OnDamageTaken(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType)
        {
            // Damage notifications run immediately before the shared reflection stage. Delay the
            // removal so the hit that consumes the last temporary HP still reflects, then prevent
            // every subsequent hit from using the expired barrier.
            RemoveWhenGuardianWardPoolEnds(defender, delayUntilAfterDamageResolution: true);
        }

        private void RemoveWhenGuardianWardPoolEnds(uint creature, bool delayUntilAfterDamageResolution)
        {
            if (TemporaryHitPointEffects.IsActivePoolFromSource(creature, "GUARDIAN_WARD", Source))
                return;

            if (delayUntilAfterDamageResolution)
            {
                DelayCommand(0f, () => StatusEffect.RemoveStatusEffect(creature, GetType(), Source, false));
            }
            else
            {
                StatusEffect.RemoveStatusEffect(creature, GetType(), Source, false);
            }
        }
    }
}
