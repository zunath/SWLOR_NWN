using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
            var offHand = GetItemInSlot(InventorySlot.LeftHand, activator);
            var rightHandType = GetBaseItemType(weapon);
            var leftHandType = GetBaseItemType(offHand);

            if (Item.FinesseVibrobladeBaseItemTypes.Contains(rightHandType) || 
                Item.FinesseVibrobladeBaseItemTypes.Contains(leftHandType))
            {
                return string.Empty;
            }
            else
                return "This is a finesse vibroblade ability.";
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0.0f;
            var inflictPoison = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 6.0f;
                    if (d2() == 1) inflictPoison = true;
                    break;
                case 2:
                    dmg = 7.5f;
                    if (d4() > 1) inflictPoison = true;
                    break;
                case 3:
                    dmg = 11.0f;
                    inflictPoison = true;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            CombatPoint.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var perception = GetAbilityModifier(AbilityType.Perception, activator);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);

            var damage = Combat.CalculateDamage(dmg, perception, defense, vitality, 0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
            if (inflictPoison) StatusEffect.Apply(activator, target, StatusEffectType.Poison, 60f);
        }

        private static void PoisonStab1(AbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab1, PerkType.PoisonStab)
                .Name("Poison Stab I")
                .HasRecastDelay(RecastGroup.PoisonStab, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void PoisonStab2(AbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab2, PerkType.PoisonStab)
                .Name("Poison Stab II")
                .HasRecastDelay(RecastGroup.PoisonStab, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void PoisonStab3(AbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab3, PerkType.PoisonStab)
                .Name("Poison Stab III")
                .HasRecastDelay(RecastGroup.PoisonStab, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}