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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class DominateWeakMindAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DominateWeakMind1(builder);

            return builder.Build();
        }

        private static void DominateWeakMind1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DominateWeakMind1, PerkType.DominateWeakMind)
                .Name("Dominate Weak Mind")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.DominateWeakMind, 90f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(DominateWeakMind1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void DominateWeakMind1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                8,
                typeof(FoggyMindStatusEffect),
                false,
                new[] { typeof(DominateWeakMind1StatusEffect) },
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
        }

    }
}
