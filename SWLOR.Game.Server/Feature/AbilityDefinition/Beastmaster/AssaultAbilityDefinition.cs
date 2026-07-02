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
    public sealed class AssaultAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Assault1(builder);
            Assault2(builder);
            Assault3(builder);

            return builder.Build();
        }

        private static void Assault1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Assault1, PerkType.Assault)
                .Name("Assault I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.ForceLeap)
                .HasRecastDelay(RecastGroup.Assault, 15f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(Assault1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void Assault2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Assault2, PerkType.Assault)
                .Name("Assault II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.ForceLeap)
                .HasRecastDelay(RecastGroup.Assault, 15f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(Assault2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void Assault3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Assault3, PerkType.Assault)
                .Name("Assault III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.ForceLeap)
                .HasRecastDelay(RecastGroup.Assault, 15f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(Assault3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void Assault1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                10,
                10,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);

            ApplySelfStatus(activator, typeof(Assault1StatusEffect));
        }

        private static void Assault2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                20,
                10,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);

            ApplySelfStatus(activator, typeof(Assault2StatusEffect));
        }

        private static void Assault3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                32,
                10,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);

            ApplySelfStatus(activator, typeof(Assault3StatusEffect));
        }

        private static void ApplySelfStatus(uint activator, Type statusEffect)
        {
            StatusEffect.ApplyStatusEffect(activator, activator, statusEffect, 30f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), activator);
        }

    }
}
