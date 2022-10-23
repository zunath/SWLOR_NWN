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
                    dmg = 30 + (willBonus * 1);
                    break;
                case 2:
                    dmg = 45 + (willBonus * 2);
                    break;
                case 3:
                    dmg = 60 + (willBonus * 3);
                    break;
               
           

            }

            var Temphp = 0;
             switch (level)
            {
                case 1:
                    Temphp = 40 + (willBonus * 2);
                    break;
                case 2:
                    Temphp = 55 + (willBonus * 4);
                    break;
                case 3:
                    Temphp = 70 + (willBonus * 6);
                    break;
               
           

            }
            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);



            ApplyEffectToObject(DurationType.Instant, EffectTemporaryHitpoints(Temphp), OBJECT_SELF, 60f);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Starburst_Green), target);
        }

           private static void ForceDeath1(AbilityBuilder builder)
           {
            builder.Create(FeatType.ForceDeath1, PerkType.ForceDeath)
                .Name("Force Death I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceDeath, 6f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementFP(6)
                .IsCastedAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
              
           }

           private static void ForceDeath2(AbilityBuilder builder)
           {
            builder.Create(FeatType.ForceDeath2, PerkType.ForceDeath)
                .Name("Force Death II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceDeath, 6f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementFP(6)
                .IsCastedAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
                
           }
 
           private static void ForceDeath3(AbilityBuilder builder)
           {
            builder.Create(FeatType.ForceDeath3, PerkType.ForceDeath)
                .Name("Force Death III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceDeath, 6f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementFP(6)
                .IsCastedAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
               .HasImpactAction(ImpactAction);
           }
        
    }
}
     

