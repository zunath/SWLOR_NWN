using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TranquilizedStatusEffect : StatusEffectBase
    {
        public override string Name => "Tranquilized";
        public override EffectIconType Icon => EffectIconType.TranquilizedStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control | StatusEffectCategory.HardCrowdControl;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;

        public override string CanApply(uint creature)
        {
            if (StatusEffect.HasStatusEffect(creature, GetType()))
                return "Target is already tranquilized.";

            return Ability.HasHardCrowdControlImmunity(creature, ImmunityType.Sleep)
                ? "Target is temporarily immune to tranquilization."
                : string.Empty;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplySleep(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplySleep(creature, GetDurationSeconds(DurationTicks));
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            if (damage <= 0)
                return;

            StatusEffect.RemoveStatusEffect(defender, GetType(), Source);
        }

        protected override void Remove(uint creature)
        {
            if (IsBeingReplaced)
                return;

            Ability.ApplyPostControlImmunity(
                creature,
                SecondsSinceNaturalExpiration,
                ImmunityType.Sleep);

            var attackPenalty = Stat.GetStatAdjustment(Source, StatType.TranquilizeExpiredAttackPercentAdjustment);
            var duration = Math.Max(
                0f,
                Stat.GetStatAdjustment(Source, StatType.TranquilizeExpiredAttackDurationSeconds) -
                SecondsSinceNaturalExpiration);
            if (attackPenalty == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.AttackPercentAdjustment,
                attackPenalty,
                duration,
                StatType.TranquilizeExpiredAttackPercentAdjustment);
        }

        private void ApplySleep(uint creature, float duration)
        {
            var effect = TagNativeEffect(IgnoreEffectImmunity(EffectSleep()));
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
        }
    }
}
