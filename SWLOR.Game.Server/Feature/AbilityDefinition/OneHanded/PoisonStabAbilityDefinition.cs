//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
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

        private static string Validation(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (Item.FinesseVibrobladeBaseItemTypes.Contains(GetBaseItemType(weapon))
                && (GetBaseItemType((GetItemInSlot(InventorySlot.LeftHand))) == Core.NWScript.Enum.Item.BaseItem.SmallShield ||
                    GetBaseItemType((GetItemInSlot(InventorySlot.LeftHand))) == Core.NWScript.Enum.Item.BaseItem.LargeShield ||
                    GetBaseItemType((GetItemInSlot(InventorySlot.LeftHand))) == Core.NWScript.Enum.Item.BaseItem.TowerShield ||
                    GetBaseItemType((GetItemInSlot(InventorySlot.LeftHand))) == Core.NWScript.Enum.Item.BaseItem.Invalid))
            {
                return "This is a one-handed ability.";
            }
            else 
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level)
        {
            var damage = 0;
            var inflictPoision = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    damage = d6();
                    if (d2() == 1) inflictPoision = true;
                    break;
                case 2:
                    damage = d6(2);
                    if (d4() > 1) inflictPoision = true;
                    break;
                case 3:
                    damage = d6(3);
                    inflictPoision = true;
                    break;
                default:
                    break;
            }

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
            if (inflictPoision) StatusEffect.Apply(activator, target, StatusEffectType.Poison, 60f);

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void PoisonStab1(AbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab1, PerkType.PoisonStab)
                .Name("Poison Stab I")
                .HasRecastDelay(RecastGroup.PoisonStab, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation((activator, target, level) =>
                {
                    return Validation(activator, target, level);
                })
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
        private static void PoisonStab2(AbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab2, PerkType.PoisonStab)
                .Name("Poison Stab II")
                .HasRecastDelay(RecastGroup.PoisonStab, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasCustomValidation((activator, target, level) =>
                {
                    return Validation(activator, target, level);
                })
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
        private static void PoisonStab3(AbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab3, PerkType.PoisonStab)
                .Name("Poison Stab III")
                .HasRecastDelay(RecastGroup.PoisonStab, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation((activator, target, level) =>
                {
                    return Validation(activator, target, level);
                })
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
    }
}