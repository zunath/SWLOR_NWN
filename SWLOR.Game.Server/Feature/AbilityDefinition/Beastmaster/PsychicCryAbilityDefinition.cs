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
    public sealed class PsychicCryAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PsychicCry1(builder);
            PsychicCry2(builder);
            PsychicCry3(builder);

            return builder.Build();
        }

        private static void PsychicCry1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PsychicCry1, PerkType.PsychicCry)
                .Name("Psychic Cry I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.PsychicCry, 24f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(PsychicCry1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void PsychicCry2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PsychicCry2, PerkType.PsychicCry)
                .Name("Psychic Cry II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.PsychicCry, 24f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(PsychicCry2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(7);
        }

        private static void PsychicCry3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PsychicCry3, PerkType.PsychicCry)
                .Name("Psychic Cry III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.PsychicCry, 24f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(PsychicCry3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void PsychicCry1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                0,
                30,
                typeof(PsychicCry1StatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small,
                areaVisualEffect: VisualEffect.None);
        }

        private static void PsychicCry2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                0,
                30,
                typeof(PsychicCry2StatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind);
        }

        private static void PsychicCry3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                0,
                30,
                typeof(PsychicCry3StatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind);
        }

    }
}
