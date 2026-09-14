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
    public sealed class BiteAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Bite1(builder);
            Bite2(builder);
            Bite3(builder);

            return builder.Build();
        }

        private static void Bite1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Bite1, PerkType.Bite)
                .Name("Bite I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleStrike)
                .HasRecastDelay(RecastGroup.Bite, 8f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(Bite1ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void Bite2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Bite2, PerkType.Bite)
                .Name("Bite II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleStrike)
                .HasRecastDelay(RecastGroup.Bite, 8f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(Bite2ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void Bite3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Bite3, PerkType.Bite)
                .Name("Bite III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleStrike)
                .HasRecastDelay(RecastGroup.Bite, 8f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(Bite3ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void Bite1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                12,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);
        }

        private static void Bite2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                22,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);
        }

        private static void Bite3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                36,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);
        }

    }
}
