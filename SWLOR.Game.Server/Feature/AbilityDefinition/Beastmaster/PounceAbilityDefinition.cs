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
    public sealed class PounceAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Pounce1(builder);
            Pounce2(builder);

            return builder.Build();
        }

        private static void Pounce1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Pounce1, PerkType.Pounce)
                .Name("Pounce I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.ForceLeap)
                .HasRecastDelay(RecastGroup.Pounce, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(Pounce1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void Pounce2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Pounce2, PerkType.Pounce)
                .Name("Pounce II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.ForceLeap)
                .HasRecastDelay(RecastGroup.Pounce, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(Pounce2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void Pounce1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeapAndInterrupt(activator, target);

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                14,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);
        }

        private static void Pounce2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeapAndInterrupt(activator, target);

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                24,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);
        }

        private static void LeapAndInterrupt(uint activator, uint target)
        {
            if (!GetIsObjectValid(target))
                return;

            AssignCommand(target, () => ClearAllActions());
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Summon_Monster_1), activator);
            AssignCommand(activator, () => ActionJumpToObject(target));
        }

    }
}
