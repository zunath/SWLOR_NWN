﻿//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceLightningAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceLightning1(builder);
            ForceLightning2(builder);
            ForceLightning3(builder);
            ForceLightning4(builder);

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0.0f;

            switch (level)
            {
                case 1:
                    dmg = 6.0f;
                    break;
                case 2:
                    dmg = 8.5f;
                    break;
                case 3:
                    dmg = 12.0f;
                    break;
                case 4:
                    dmg = 13.5f;
                    break;
            }

            var willpower = GetAbilityModifier(AbilityType.Willpower, activator);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical);
            var targetWillpower = GetAbilityModifier(AbilityType.Willpower, target);
            var damage = Combat.CalculateDamage(dmg, willpower, defense, targetWillpower, false);

            var elecBeam = EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Hand);
            
            AssignCommand(activator, () =>
            {
                PlaySound("frc_lghtning");
                ActionPlayAnimation(Animation.CastOutAnimation, 1.0f,4.0f);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                ApplyEffectToObject(DurationType.Temporary, elecBeam, target, 4.0f);
            });

            
            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void ForceLightning1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning1, PerkType.ForceLightning)
                .Name("Force Lightning I")
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .RequirementFP(2)
                .IsCastedAbility()
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning2, PerkType.ForceLightning)
                .Name("Force Lightning II")
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .RequirementFP(3)
                .IsCastedAbility()
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning3, PerkType.ForceLightning)
                .Name("Force Lightning III")
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .RequirementFP(4)
                .IsCastedAbility()
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning4, PerkType.ForceLightning)
                .Name("Force Lightning IV")
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .RequirementFP(5)
                .IsCastedAbility()
                .HasImpactAction(ImpactAction);
        }
    }
}