using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded
{
    public class BackstabAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            Backstab1(builder);
            Backstab2(builder);
            Backstab3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var rightHandType = GetBaseItemType(weapon);

            if (Item.FinesseVibrobladeBaseItemTypes.Contains(rightHandType))
            {
                return string.Empty;
            }
            else
                return "A finesse vibroblade must be equipped in your right hand to use this ability.";
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;

            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 14;
                    break;
                case 2:
                    dmg = 30;
                    break;
                case 3:
                    dmg = 45;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            if (abs((int)(GetFacing(activator) - GetFacing(target))) > 200f ||
                abs((int)(GetFacing(activator) - GetFacing(target))) < 160f ||
                GetDistanceBetween(activator, target) > 5f)
            {
                dmg /= 2;
            }

            CombatPoint.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.OneHanded);
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

            AssignCommand(activator, () => ActionPlayAnimation(Animation.Backstab));
            Enmity.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private static void Backstab1(AbilityBuilder builder)
        {
            builder.Create(FeatType.Backstab1, PerkType.Backstab)
                .Name("Backstab I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Backstab, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void Backstab2(AbilityBuilder builder)
        {
            builder.Create(FeatType.Backstab2, PerkType.Backstab)
                .Name("Backstab II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Backstab, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void Backstab3(AbilityBuilder builder)
        {
            builder.Create(FeatType.Backstab3, PerkType.Backstab)
                .Name("Backstab III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Backstab, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}