using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ToxinStatusEffect : StatusEffectBase
    {
        public override string Name => "Toxin";
        public override EffectIconType Icon => EffectIconType.ToxinStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Poison;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 6f;

        protected override void Tick(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            var agility = GetIsObjectValid(Source) ? GetAbilityModifier(AbilityType.Agility, Source) : 0;
            var damageAmount = CalculateTickDamage(GetMaxHitPoints(creature), agility);
            damageAmount = Resistance.ApplyResistanceToDamage(creature, ResistanceType, damageAmount);
            damageAmount = Combat.ApplyDamageOverTimeTakenModifiers(creature, damageAmount, CombatDamageType.Poison, out var targetStatusDamageAdjustment);
            damageAmount = Combat.ApplyDamageTakenModifiers(creature, damageAmount, source, CombatDamageType.Poison,
                deliveryType: CombatDamageDeliveryType.DamageOverTime, targetStatusDamagePercentAdjustment: targetStatusDamageAdjustment);
            if (damageAmount <= 0)
                return;

            AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damageAmount, DamageType.Acid), creature));
        }

        /// <summary>
        /// Caps percentage damage before mitigation, retaining Toxin's 3:2 base potency over Bleed.
        /// </summary>
        public static int CalculateTickDamage(int targetMaxHP, int agilityModifier)
        {
            var damageCap = 30 + 3 * Math.Max(0, agilityModifier);
            return Math.Min(GameMath.PercentOf(targetMaxHP, 6), damageCap);
        }
    }
}
