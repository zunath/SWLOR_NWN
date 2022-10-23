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
    public class ForceDeathAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {

            var builder = new AbilityBuilder();
            ForceDeath1(builder);
            ForceDeath2(builder);
            ForceDeath3(builder);

            return builder.Build();

        }
        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            var willBonus = GetAbilityModifier(AbilityType.Willpower, activator);
            switch (level)
            {
                case 1:
                    dmg = 12 + (willBonus * 4);
                    break;
                case 2:
                    dmg = 19 + (willBonus * 9);
                    break;
                case 3:
                    dmg = 28 + (willBonus * 14);
                    break;
                case 4:
                    dmg = 40 + (willBonus * 18);
                    break;

           

            }







            

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Force);

            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            var delay = GetDistanceBetweenLocations(GetLocation(activator), targetLocation) / 18.0f + 0.35f;

            AssignCommand(activator, () =>
            {
                PlaySound("plr_force_blast");
                DoFireball(target);
            });

            DelayCommand(delay, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Silence), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.VFX_IMP_KIN_L), target);
            });
            
            Enmity.ModifyEnmity(activator, target, level * 150 + damage);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);

            
        }

           private static void ForceDeath1(AbilityBuilder builder)
           {
            builder.Create(FeatType.ForceDeath1, PerkType.ForceDeath)
                .Name("Force Death I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceBody Death, 6f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
              
           }

           private static void ForceDeath2(AbilityBuilder builder)
           {
            builder.Create(FeatType.ForceDeath2, PerkType.ForceDeath)
                .Name("Force Death II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Benevolence, 6f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
                
           }
 
           private static void ForceDeath3(AbilityBuilder builder)
           {
            builder.Create(FeatType.ForceDeath3, PerkType.ForceDeath)
                .Name("Force Death III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Benevolence, 6f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
               .HasImpactAction(ImpactAction);
           }
        
    }
}
     

