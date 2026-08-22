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
    public sealed class IceBreathAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            IceBreath1(builder);
            IceBreath2(builder);
            IceBreath3(builder);

            return builder.Build();
        }

        private static void IceBreath1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.IceBreath1, PerkType.IceBreath)
                .Name("Ice Breath I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.IceBreath, 15f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(IceBreath1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void IceBreath2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.IceBreath2, PerkType.IceBreath)
                .Name("Ice Breath II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.IceBreath, 15f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(IceBreath2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void IceBreath3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.IceBreath3, PerkType.IceBreath)
                .Name("Ice Breath III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.IceBreath, 15f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(IceBreath3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void IceBreath1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                10,
                4,
                typeof(HamstringStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Ice,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Frost,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Icestorm);
        }

        private static void IceBreath2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                14,
                5,
                typeof(HamstringStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Ice,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Frost,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Icestorm);
        }

        private static void IceBreath3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                18,
                10,
                typeof(ImmobilizedStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Ice,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Frost,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Icestorm);
        }

    }
}
