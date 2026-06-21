using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public sealed class KoltoMistAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            KoltoMist1(builder);
            KoltoMist2(builder);

            return builder.Build();
        }

        private static void KoltoMist1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.KoltoMist1, PerkType.KoltoMist)
                .Name("Kolto Mist I")
                .Level(1)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.KoltoMist, 30f)
                .SkillType(SkillType.FirstAid)
                .IsAreaAbility()
                .HasImpactAction(KoltoMist1ImpactAction)
                .HasTargetingSphere(
                    Spell.KoltoMist1,
                    3f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6)
                .RequirementItem("med_supplies");
        }

        private static void KoltoMist2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.KoltoMist2, PerkType.KoltoMist)
                .Name("Kolto Mist II")
                .Level(2)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.KoltoMist, 30f)
                .SkillType(SkillType.FirstAid)
                .IsAreaAbility()
                .HasImpactAction(KoltoMist2ImpactAction)
                .HasTargetingSphere(
                    Spell.KoltoMist2,
                    3f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(7)
                .RequirementItem("med_supplies");
        }

        private static void KoltoMist1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyKoltoMist(activator, 7f);
        }

        private static void KoltoMist2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyKoltoMist(activator, 12f);
        }

        private static void ApplyKoltoMist(uint activator, float totalPercent)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 3f))
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    friendly,
                    new KoltoMistHealingStatusEffect(totalPercent, 4),
                    12f);
                FirstAidTreatmentAdjustments.ApplyTraumaMedicRiders(activator, friendly);
                FirstAidTreatmentAdjustments.ApplyMedicalVisualEffect(friendly);
            }
        }
    }
}
