using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class CrushingSlamAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CrushingSlam1(builder);
            CrushingSlam2(builder);
            CrushingSlam3(builder);

            return builder.Build();
        }

        private static void CrushingSlam1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CrushingSlam1, PerkType.CrushingSlam)
                .Name("Crushing Slam I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleThrust)
                .HasRecastDelay(RecastGroup.CrushingSlam, 24f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(CrushingSlam1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void CrushingSlam2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CrushingSlam2, PerkType.CrushingSlam)
                .Name("Crushing Slam II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleThrust)
                .HasRecastDelay(RecastGroup.CrushingSlam, 24f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(CrushingSlam2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void CrushingSlam3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CrushingSlam3, PerkType.CrushingSlam)
                .Name("Crushing Slam III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleThrust)
                .HasRecastDelay(RecastGroup.CrushingSlam, 24f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(CrushingSlam3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(9);
        }

        private static void CrushingSlam1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                10,
                15,
                typeof(DazedStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Imp_Dust_Explosion,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Screen_Shake);
        }

        private static void CrushingSlam2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                14,
                15,
                typeof(DazedStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Imp_Dust_Explosion,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Screen_Shake);
        }

        private static void CrushingSlam3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                18,
                15,
                typeof(DazedStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Imp_Dust_Explosion,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Screen_Shake);
        }

    }
}
