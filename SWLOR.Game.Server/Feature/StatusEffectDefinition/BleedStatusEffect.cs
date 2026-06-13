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

        protected override void Tick(uint creature)
        {
            var damageAmount = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(creature) * 0.04f));
            var damageAdjustment = Stat.GetStatAdjustment(Source, StatType.OutgoingBleedingDamagePercentAdjustment);
            if (damageAdjustment != 0)
            {
                damageAmount = Math.Max(1, damageAmount + (int)Math.Ceiling(damageAmount * (damageAdjustment / 100f)));
            }

            var resistanceType = Resistance.IsValidResistanceType(AppliedResistanceType)
                ? AppliedResistanceType
                : ResistanceType;
            damageAmount = Resistance.ApplyResistanceToDamage(creature, resistanceType, damageAmount);
            damageAmount = Combat.ApplyDamageOverTimeTakenModifiers(creature, damageAmount, CombatDamageType.Physical);
            if (damageAmount <= 0)
                return;

            var source = GetIsObjectValid(Source) ? Source : creature;
            AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damageAmount), creature));

            var location = GetLocation(creature);
            var placeable = CreateObject(ObjectType.Placeable, "plc_bloodstain", location);
            DestroyObject(placeable, 48.0f);
        }
    }
}
