using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public static class FirstAidTreatmentAdjustments
    {
        public static void ApplyMedicalScaledHeal(
            uint source,
            uint target,
            float percent,
            AbilityType scalingAbility = AbilityType.Willpower,
            float multiplier = 1f,
            VisualEffect visualEffect = VisualEffect.Vfx_Imp_Healing_M_Silent)
        {
            var amount = AbilityEffectScaling.CalculateScaledPercentOfMaxHP(source, target, percent, scalingAbility, multiplier);
            amount = Stat.ApplyOutgoingAbilityHealingAdjustment(source, amount);
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyMedicalVisualEffect(target, visualEffect);
        }

        public static void ApplyActivatedMedicalScaledHeal(
            uint source,
            uint target,
            float percent,
            AbilityType scalingAbility = AbilityType.Willpower,
            float multiplier = 1f)
        {
            var amount = AbilityEffectScaling.CalculateScaledPercentOfMaxHP(source, target, percent, scalingAbility, multiplier);
            amount = Stat.ApplyOutgoingAbilityHealingAdjustment(source, amount);
            amount = Ability.ApplyCombatReadinessToActivatedAbilityMagnitude(source, amount);
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyMedicalVisualEffect(target);
        }

        public static void ApplyMedicalVisualEffect(
            uint target,
            VisualEffect visualEffect = VisualEffect.Vfx_Imp_Healing_M_Silent)
        {
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visualEffect), target);
        }

        public static void GrantCombatPoint(uint activator)
        {
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid);
        }

        public static void GrantCombatPointIfApplied(uint activator, bool applied)
        {
            if (applied)
                GrantCombatPoint(activator);
        }

        public static float ApplyStimDurationBonus(uint source, float durationSeconds)
        {
            if (durationSeconds <= 0f || !GetIsObjectValid(source))
                return durationSeconds;

            var adjustment = Stat.GetStatAdjustment(source, StatType.StimPackDurationPercentAdjustment);
            if (adjustment <= 0)
                return durationSeconds;

            return durationSeconds + durationSeconds * (adjustment / 100f);
        }

        public static void ApplyCombatPharmacologyStimRiders(uint source, uint target)
        {
            var coagulantRank = Stat.GetStatAdjustment(source, StatType.CombatPharmacologyStimCoagulantRank);
            if (coagulantRank <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                source,
                target,
                coagulantRank >= 2 ? typeof(Coagulant2StatusEffect) : typeof(Coagulant1StatusEffect),
                120f);
        }

        public static void ApplyTraumaMedicRiders(uint source, uint target)
        {
            if (Stat.GetStatAdjustment(source, StatType.TraumaMedicEmergencySealant) <= 0)
                return;

            var removedAilment = false;
            if (StatusEffect.HasStatusEffect(target, typeof(BleedStatusEffect)))
            {
                StatusEffect.RemoveStatusEffect(target, typeof(BleedStatusEffect), false);
                removedAilment = true;
            }
            else if (StatusEffect.HasStatusEffect(target, typeof(BurnStatusEffect)))
            {
                StatusEffect.RemoveStatusEffect(target, typeof(BurnStatusEffect), false);
                removedAilment = true;
            }

            if (!removedAilment)
                return;

            StatusEffect.ApplyStatusEffect(source, target, typeof(EmergencySealant1StatusEffect), 30f);
        }
    }
}
