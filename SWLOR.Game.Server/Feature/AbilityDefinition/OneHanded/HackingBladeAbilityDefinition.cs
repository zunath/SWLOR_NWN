using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded
{
    public class HackingBladeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            HackingBlade1(builder);
            HackingBlade2(builder);
            HackingBlade3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (Item.VibrobladeBaseItemTypes.Contains(GetBaseItemType(weapon))
                && (GetBaseItemType((GetItemInSlot(InventorySlot.LeftHand))) == BaseItem.SmallShield ||
                    GetBaseItemType((GetItemInSlot(InventorySlot.LeftHand))) == BaseItem.LargeShield ||
                    GetBaseItemType((GetItemInSlot(InventorySlot.LeftHand))) == BaseItem.TowerShield ||
                    GetBaseItemType((GetItemInSlot(InventorySlot.LeftHand))) == BaseItem.Invalid))
            {
                return "This is a one-handed ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0.0f;
            var inflictBleed = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 6.5f;
                    if (d2() == 1) inflictBleed = true;
                    break;
                case 2:
                    dmg = 8.0f;
                    if (d4() > 1) inflictBleed = true;
                    break;
                case 3:
                    dmg = 11.5f;
                    inflictBleed = true;
                    break;
                default:
                    break;
            }

            var might = GetAbilityModifier(AbilityType.Might, activator);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = Combat.CalculateDamage(dmg, might, defense, vitality, false);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
            if (inflictBleed) StatusEffect.Apply(activator, target, StatusEffectType.Bleed, 60f);

            CombatPoint.AddCombatPoint(activator, target, SkillType.OneHanded, 3);
        }

        private static void HackingBlade1(AbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade1, PerkType.HackingBlade)
                .Name("Hacking Blade I")
                .HasRecastDelay(RecastGroup.HackingBlade, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void HackingBlade2(AbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade2, PerkType.HackingBlade)
                .Name("Hacking Blade II")
                .HasRecastDelay(RecastGroup.HackingBlade, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void HackingBlade3(AbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade3, PerkType.HackingBlade)
                .Name("Hacking Blade III")
                .HasRecastDelay(RecastGroup.HackingBlade, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}