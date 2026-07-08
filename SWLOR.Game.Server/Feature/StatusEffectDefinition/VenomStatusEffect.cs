using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class VenomStatusEffect : StatusEffectBase
    {
        private const int DamagePerTick = 8;

        public override string Name => "Venom";
        public override EffectIconType Icon => EffectIconType.VenomStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Venom;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;
        public override ResistanceType ResistanceType => ResistanceType.Poison;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 6f;

        protected override void Apply(uint creature, int durationTicks)
        {
            var requiredCategory = (StatusEffectCategory)Stat.GetStatAdjustment(
                Source,
                StatType.SourceStatusHealingReceivedRequiredCategory);
            if ((Categories & requiredCategory) == 0)
                return;

            var healingAdjustment = Stat.GetStatAdjustment(Source, StatType.SourceStatusHealingReceivedPercentAdjustment);
            if (healingAdjustment != 0)
            {
                StatGroup.Stats[StatType.HealingReceivedPercentAdjustment] = healingAdjustment;
            }
        }

        protected override void Tick(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            var damageAmount = Combat.ApplyDamageTypeDealtModifiers(source, DamagePerTick, CombatDamageType.Poison);
            damageAmount = Resistance.ApplyResistanceToDamage(creature, ResistanceType, damageAmount);
            damageAmount = Combat.ApplyDamageOverTimeTakenModifiers(creature, damageAmount, CombatDamageType.Poison);
            damageAmount = Combat.ApplyDamageTakenModifiers(
                creature,
                damageAmount,
                source,
                CombatDamageType.Poison,
                CombatDamageDeliveryType.DamageOverTime);
            if (damageAmount <= 0)
                return;

            AssignCommand(
                source,
                () => ApplyEffectToObject(
                    DurationType.Instant,
                    EffectDamage(damageAmount, CombatDamageType.Poison.GetNWScriptDamageType()),
                    creature));
        }
    }
}
