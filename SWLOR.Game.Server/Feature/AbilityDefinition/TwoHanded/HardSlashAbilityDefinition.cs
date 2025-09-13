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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class HardSlashAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            HardSlash1(builder);
            HardSlash2(builder);
            HardSlash3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.HeavyVibrobladeBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a heavy vibroblade ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;

            switch (level)
            {
                case 1:
                    dmg = 16;
                    break;
                case 2:
                    dmg = 24;
                    break;
                case 3:
                    dmg = 38;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.TwoHanded);
            
            var attackerStat = GetAbilityScore(activator, AbilityType.Might);
            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.TwoHanded);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack, 
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            AssignCommand(activator, () => ActionPlayAnimation(Animation.DoubleStrike));

            CombatPoint.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
            Enmity.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private static void HardSlash1(AbilityBuilder builder)
        {
            builder.Create(FeatType.HardSlash1, PerkType.HardSlash)
                .Name("Hard Slash I")
                .Level(1)
                .HasRecastDelay(RecastGroup.HardSlash, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void HardSlash2(AbilityBuilder builder)
        {
            builder.Create(FeatType.HardSlash2, PerkType.HardSlash)
                .Name("Hard Slash II")
                .Level(2)
                .HasRecastDelay(RecastGroup.HardSlash, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void HardSlash3(AbilityBuilder builder)
        {
            builder.Create(FeatType.HardSlash3, PerkType.HardSlash)
                .Name("Hard Slash III")
                .Level(3)
                .HasRecastDelay(RecastGroup.HardSlash, 60f)
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