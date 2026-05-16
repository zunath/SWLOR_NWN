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
        public override EffectIconType Icon => EffectIconType.Sleep;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;

        public override string CanApply(uint creature)
        {
            return Ability.HasTemporaryImmunity(creature, ImmunityType.Sleep)
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
            var attackPenalty = Stat.GetStatAdjustment(Source, StatType.TranquilizeExpiredAttackPercentAdjustment);
            var duration = Stat.GetStatAdjustment(Source, StatType.TranquilizeExpiredAttackDurationSeconds);
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
            var effect = TagEffect(IgnoreEffectImmunity(EffectSleep()), Id);
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
            Ability.ApplyTemporaryImmunity(creature, duration, ImmunityType.Sleep);
        }
    }
}
