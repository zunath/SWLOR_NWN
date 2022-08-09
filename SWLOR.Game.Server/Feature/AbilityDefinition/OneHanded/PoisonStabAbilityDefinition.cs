using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded
{
    public class PoisonStabAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            PoisonStab1(builder);
            PoisonStab2(builder);
            PoisonStab3(builder);

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
            var inflictPoison = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 8;
                    if (d2() == 1) inflictPoison = true;
                    break;
                case 2:
                    dmg = 18;
                    if (d4() > 1) inflictPoison = true;
                    break;
                case 3:
                    dmg = 28;
                    inflictPoison = true;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.OneHanded);

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
            if (inflictPoison) 
                StatusEffect.Apply(activator, target, StatusEffectType.Poison, 60f);

            Enmity.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private static void PoisonStab1(AbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab1, PerkType.PoisonStab)
                .Name("Poison Stab I")
                .Level(1)
                .HasRecastDelay(RecastGroup.PoisonStab, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void PoisonStab2(AbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab2, PerkType.PoisonStab)
                .Name("Poison Stab II")
                .Level(2)
                .HasRecastDelay(RecastGroup.PoisonStab, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void PoisonStab3(AbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab3, PerkType.PoisonStab)
                .Name("Poison Stab III")
                .Level(3)
                .HasRecastDelay(RecastGroup.PoisonStab, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}