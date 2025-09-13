//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded
{
    public class RiotBladeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            RiotBlade1(builder);
            RiotBlade2(builder);
            RiotBlade3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var rightHandType = GetBaseItemType(weapon);
            
            if (Item.VibrobladeBaseItemTypes.Contains(rightHandType))
            {
                return string.Empty;
            }
            else
                return "A vibroblade must be equipped in your right hand to use this ability.";
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;



            switch (level)
            {
                case 1:
                    dmg = 10;
                    break;
                case 2:
                    dmg = 20;
                    break;
                case 3:
                    dmg = 30;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            CombatPoint.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var might = GetAbilityScore(activator, AbilityType.Might);
            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.OneHanded);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = Combat.CalculateDamage(attack, dmg, might, defense, vitality, 0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            AssignCommand(activator, () => ActionPlayAnimation(Animation.RiotBlade));

            Enmity.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private static void RiotBlade1(AbilityBuilder builder)
        {
            builder.Create(FeatType.RiotBlade1, PerkType.RiotBlade)
                .Name("Riot Blade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.RiotBlade, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void RiotBlade2(AbilityBuilder builder)
        {
            builder.Create(FeatType.RiotBlade2, PerkType.RiotBlade)
                .Name("Riot Blade II")
                .Level(2)
                .HasRecastDelay(RecastGroup.RiotBlade, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void RiotBlade3(AbilityBuilder builder)
        {
            builder.Create(FeatType.RiotBlade3, PerkType.RiotBlade)
                .Name("Riot Blade III")
                .Level(3)
                .HasRecastDelay(RecastGroup.RiotBlade, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}