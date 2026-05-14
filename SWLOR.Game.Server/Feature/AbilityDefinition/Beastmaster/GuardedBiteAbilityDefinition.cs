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
    public sealed class GuardedBiteAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            GuardedBite1(builder);
            GuardedBite2(builder);
            GuardedBite3(builder);

            return builder.Build();
        }

        private static void GuardedBite1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardedBite1, PerkType.GuardedBite)
                .Name("Guarded Bite I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GuardedBite, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(GuardedBite1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void GuardedBite2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardedBite2, PerkType.GuardedBite)
                .Name("Guarded Bite II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GuardedBite, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(GuardedBite2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void GuardedBite3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardedBite3, PerkType.GuardedBite)
                .Name("Guarded Bite III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GuardedBite, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(GuardedBite3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void GuardedBite1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                12,
                10,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);

            foreach (var friendly in new[] { activator })
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(GuardedBite1SelfStatusEffect), 10f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);
            }
        }

        private static void GuardedBite2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                22,
                10,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);

            foreach (var friendly in new[] { activator })
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(GuardedBite2SelfStatusEffect), 10f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);
            }
        }

        private static void GuardedBite3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                34,
                10,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);

            foreach (var friendly in new[] { activator })
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(GuardedBite3SelfStatusEffect), 10f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);
            }
        }

    }
}
