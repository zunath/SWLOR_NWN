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
    public sealed class PoisonBreathAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PoisonBreath1(builder);
            PoisonBreath2(builder);
            PoisonBreath3(builder);

            return builder.Build();
        }

        private static void PoisonBreath1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PoisonBreath1, PerkType.PoisonBreath)
                .Name("Poison Breath I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.PoisonBreath, 24f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(PoisonBreath1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void PoisonBreath2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PoisonBreath2, PerkType.PoisonBreath)
                .Name("Poison Breath II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.PoisonBreath, 24f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(PoisonBreath2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void PoisonBreath3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PoisonBreath3, PerkType.PoisonBreath)
                .Name("Poison Breath III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.PoisonBreath, 24f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(PoisonBreath3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void PoisonBreath1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                10,
                12,
                typeof(PoisonStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Poison,
                targetVisualEffect: VisualEffect.Vfx_Imp_Poison_S,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Gas_Explosion_Acid);
        }

        private static void PoisonBreath2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                14,
                12,
                typeof(PoisonStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Poison,
                targetVisualEffect: VisualEffect.Vfx_Imp_Poison_S,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Gas_Explosion_Acid);
        }

        private static void PoisonBreath3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                18,
                12,
                typeof(PoisonStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Poison,
                targetVisualEffect: VisualEffect.Vfx_Imp_Poison_S,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Gas_Explosion_Acid);
        }

    }
}
