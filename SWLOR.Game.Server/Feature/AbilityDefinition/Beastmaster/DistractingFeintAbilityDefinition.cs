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
    public sealed class DistractingFeintAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DistractingFeint1(builder);
            DistractingFeint2(builder);
            DistractingFeint3(builder);

            return builder.Build();
        }

        private static void DistractingFeint1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DistractingFeint1, PerkType.DistractingFeint)
                .Name("Distracting Feint I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.DistractingFeint, 15f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(DistractingFeint1ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void DistractingFeint2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DistractingFeint2, PerkType.DistractingFeint)
                .Name("Distracting Feint II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.DistractingFeint, 15f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(DistractingFeint2ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void DistractingFeint3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DistractingFeint3, PerkType.DistractingFeint)
                .Name("Distracting Feint III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.DistractingFeint, 15f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(DistractingFeint3ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void DistractingFeint1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyDistractingFeint(activator, target, targetLocation, typeof(DistractingFeint1StatusEffect), 350);
        }

        private static void DistractingFeint2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyDistractingFeint(activator, target, targetLocation, typeof(DistractingFeint2StatusEffect), 500);
        }

        private static void DistractingFeint3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyDistractingFeint(activator, target, targetLocation, typeof(DistractingFeint3StatusEffect), 650);
        }

        private static void ApplyDistractingFeint(
            uint activator,
            uint target,
            Location targetLocation,
            Type statusEffect,
            int baseEnmity)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                0,
                15,
                statusEffect,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);

            ApplyExtraEnmity(activator, target, baseEnmity);
        }

        private static void ApplyExtraEnmity(uint activator, uint target, int baseEnmity)
        {
            if (!GetIsObjectValid(target) || !GetIsReactionTypeHostile(target, activator))
                return;

            var enmity = Stat.ScaleEffect(baseEnmity, GetAbilityScore(activator, AbilityType.Vitality));
            Enmity.ModifyEnmity(activator, target, enmity);
        }
    }
}
