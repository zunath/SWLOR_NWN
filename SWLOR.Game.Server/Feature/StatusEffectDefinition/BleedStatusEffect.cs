using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BleedStatusEffect : StatusEffectBase
    {
        public override string Name => "Bleed";
        public override EffectIconType Icon => EffectIconType.BleedStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Bleeding;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit1 |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 6f;
        public override bool PreservesTickScheduleOnRefresh => true;

        protected override void Tick(uint creature)
        {
            var hasSource = GetIsObjectValid(Source);
            var source = hasSource ? Source : creature;
            var might = hasSource ? GetAbilityModifier(AbilityType.Might, Source) : 0;
            var perception = hasSource ? GetAbilityModifier(AbilityType.Perception, Source) : 0;
            var damageAdjustment = hasSource
                ? Stat.GetStatAdjustment(Source, StatType.OutgoingBleedingDamagePercentAdjustment)
                : 0;
            var damageAmount = CalculateTickDamage(GetMaxHitPoints(creature), might, perception, damageAdjustment);

            var resistanceType = Resistance.IsValidResistanceType(AppliedResistanceType)
                ? AppliedResistanceType
                : ResistanceType;
            damageAmount = Resistance.ApplyResistanceToDamage(creature, resistanceType, damageAmount);
            damageAmount = Combat.ApplyDamageOverTimeTakenModifiers(creature, damageAmount, CombatDamageType.Physical, out var targetStatusDamageAdjustment);
            damageAmount = Combat.ApplyDamageTakenModifiers(creature, damageAmount, source, CombatDamageType.Physical,
                deliveryType: CombatDamageDeliveryType.DamageOverTime, targetStatusDamagePercentAdjustment: targetStatusDamageAdjustment);
            if (damageAmount <= 0)
                return;

            AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damageAmount, CombatDamageType.Physical.GetNWScriptDamageType()), creature));

            var location = GetLocation(creature);
            var placeable = CreateObject(ObjectType.Placeable, "plc_bloodstain", location);
            DestroyObject(placeable, 48.0f);
        }

        /// <summary>
        /// Retains percentage damage against smaller targets, with an attacker-scaled ceiling
        /// before outgoing bonuses and target mitigation so boss HP cannot multiply potency.
        /// </summary>
        public static int CalculateTickDamage(
            int targetMaxHP, int mightModifier, int perceptionModifier, int damageAdjustment = 0)
        {
            var damageCap = 20 + 2 * Math.Max(0, Math.Max(mightModifier, perceptionModifier));
            var damage = Math.Min(GameMath.PercentOf(targetMaxHP, 4), damageCap);
            return Math.Max(1, damage + (int)Math.Ceiling(damage * (damageAdjustment / 100f)));
        }

        protected override void Remove(uint creature)
        {
            if (WasNaturallyExpired)
                Combat.ApplyBleedingStatusExpiredEffects(Source);
        }
    }
}
