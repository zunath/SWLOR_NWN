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
    public sealed class PredatorsMarkAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PredatorsMark1(builder);

            return builder.Build();
        }

        private static void PredatorsMark1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PredatorsMark1, PerkType.PredatorsMark)
                .Name("Predator's Mark I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PredatorsMark, 45f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(PredatorsMark1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void PredatorsMark1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                0,
                15,
                typeof(PredatorsMark1StatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);
        }

    }
}
