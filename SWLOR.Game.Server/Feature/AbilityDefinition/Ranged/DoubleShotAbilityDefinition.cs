using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class DoubleShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            DoubleShot1(builder);
            DoubleShot2(builder);
            DoubleShot3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.PistolBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a pistol ability.";
            }
            else
                return string.Empty;
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
                    dmg = 8;
                    break;
                case 2:
                    dmg = 18;
                    break;
                case 3:
                    dmg = 28;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Ranged);

            CombatPoint.AddCombatPoint(activator, target, SkillType.Ranged, 3);

            // First attack
            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Ranged);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack, 
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);

            // Second attack
            damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
            AssignCommand(activator, () => ActionPlayAnimation(Animation.DoubleShot));
            AssignCommand(activator, () => ActionPlayAnimation(Animation.DoubleShot));

            Enmity.ModifyEnmity(activator, target, 450 * level + damage);
        }

        private static void DoubleShot1(AbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleShot1, PerkType.DoubleShot)
                .Name("Double Shot I")
                .Level(1)
                .HasRecastDelay(RecastGroup.DoubleShot, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private static void DoubleShot2(AbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleShot2, PerkType.DoubleShot)
                .Name("Double Shot II")
                .Level(2)
                .HasRecastDelay(RecastGroup.DoubleShot, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private static void DoubleShot3(AbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleShot3, PerkType.DoubleShot)
                .Name("Double Shot III")
                .Level(3)
                .HasRecastDelay(RecastGroup.DoubleShot, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
    }
}