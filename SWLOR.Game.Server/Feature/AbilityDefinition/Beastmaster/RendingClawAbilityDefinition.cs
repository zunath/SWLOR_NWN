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
    public sealed class RendingClawAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RendingClaw1(builder);
            RendingClaw2(builder);
            RendingClaw3(builder);

            return builder.Build();
        }

        private static void RendingClaw1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RendingClaw1, PerkType.RendingClaw)
                .Name("Rending Claw I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CrossCut)
                .HasRecastDelay(RecastGroup.RendingClaw, 12f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(RendingClaw1ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void RendingClaw2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RendingClaw2, PerkType.RendingClaw)
                .Name("Rending Claw II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CrossCut)
                .HasRecastDelay(RecastGroup.RendingClaw, 12f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(RendingClaw2ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void RendingClaw3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RendingClaw3, PerkType.RendingClaw)
                .Name("Rending Claw III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CrossCut)
                .HasRecastDelay(RecastGroup.RendingClaw, 12f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(RendingClaw3ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void RendingClaw1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                10,
                12,
                typeof(BleedStatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Blood_Crt_Red);
        }

        private static void RendingClaw2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                18,
                12,
                typeof(BleedStatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Blood_Crt_Red);
        }

        private static void RendingClaw3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                28,
                12,
                typeof(BleedStatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Blood_Crt_Red);
        }

    }
}
