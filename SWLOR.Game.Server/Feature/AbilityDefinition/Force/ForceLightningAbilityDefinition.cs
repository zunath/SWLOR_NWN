//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

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
            var dmg = 0;
            var willBonus = GetAbilityModifier(AbilityType.Willpower, activator);
            switch (level)
            {
                case 1:
                    dmg = 12 + (willBonus * 1);
                    break;
                case 2:
                    dmg = 19 + (willBonus * 2);
                    break;
                case 3:
                    dmg = 28 + (willBonus * 4);
                    break;
                case 4:
                    dmg = 40 + (willBonus * 8);
                    break;
            }

          

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Force);


            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            var elecBeam = EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Hand);
            
            AssignCommand(activator, () =>
            {
                PlaySound("frc_lghtning");
                ActionPlayAnimation(Animation.CastOutAnimation, 1.0f,4.0f);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                ApplyEffectToObject(DurationType.Temporary, elecBeam, target, 4.0f);
            });

            Enmity.ModifyEnmity(activator, target, level * 150 + damage);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
        }

        private static void ForceLightning1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning1, PerkType.ForceLightning)
                .Name("Force Lightning I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceLightning, 6f)
                .HasActivationDelay(6f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning2, PerkType.ForceLightning)
                .Name("Force Lightning II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceLightning, 6f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning3, PerkType.ForceLightning)
                .Name("Force Lightning III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceLightning, 6f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementFP(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning4, PerkType.ForceLightning)
                .Name("Force Lightning IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForceLightning, 6f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementFP(10)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }
    }
}