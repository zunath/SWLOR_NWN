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
    public sealed class RampageAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Rampage1(builder);
            Rampage2(builder);

            return builder.Build();
        }

        private static void Rampage1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Rampage1, PerkType.Rampage)
                .Name("Rampage I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Rampage, 45f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(Rampage1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void Rampage2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Rampage2, PerkType.Rampage)
                .Name("Rampage II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Rampage, 45f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(Rampage2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(9);
        }

        private static void Rampage1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                10,
                12,
                null,
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small,
                areaVisualEffect: VisualEffect.None,
                maxTargets: 3);
        }

        private static void Rampage2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                14,
                12,
                null,
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small,
                areaVisualEffect: VisualEffect.None,
                maxTargets: 4);
        }

    }
}
