using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PoisonStatusEffect : StatusEffectBase
    {
        public override string Name => "Poison";
        public override EffectIconType Icon => EffectIconType.Poison;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Poison;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit1 |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 6f;

        protected override void Apply(uint creature, int durationTicks)
        {
            var attackPenalty = Stat.GetStatAdjustment(Source, StatType.OutgoingPoisonAttackPercentAdjustment);
            if (attackPenalty != 0)
            {
                StatGroup.Stats[StatType.AttackPercentAdjustment] += attackPenalty;
            }
        }

        protected override void Tick(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            var level = 1;
            var agility = GetAbilityModifier(AbilityType.Agility, source);
            var amount = Math.Max(1, Random.Next(3, 7) + agility * level);
            amount = Resistance.ApplyResistanceToDamage(creature, ResistanceType, amount);
            amount = Combat.ApplyDamageOverTimeTakenModifiers(creature, amount, CombatDamageType.Poison);
            amount = Combat.ApplyDamageTakenModifiers(creature, amount, source, CombatDamageType.Poison);
            if (amount <= 0)
                return;

            AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(amount, DamageType.Acid), creature));
            StatusEffect.ApplyStatusEffect(source, creature, typeof(PoisonDefensePenaltyStatusEffect), 6f);
        }

        protected override void Remove(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            StatusEffect.RemoveStatusEffect(creature, typeof(PoisonDefensePenaltyStatusEffect), source, false);
        }
    }
}
