//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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

            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    damage = d8(2);
                    break;
                case 2:
                    damage = d8(3);
                    break;
                case 3:
                    damage = d8(4);
                    break;
                default:
                    break;
            }

            if (abs((int) (GetFacing(activator) - GetFacing(target))) > 200f ||
                abs((int)(GetFacing(activator) - GetFacing(target))) < 160f ||
                GetDistanceBetween(activator, target) > 5f)
            {
                damage /= 2;
            }

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void Backstab1(AbilityBuilder builder)
        {
            builder.Create(FeatType.Backstab1, PerkType.Backstab)
                .Name("Backstab I")
                .HasRecastDelay(RecastGroup.Backstab, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .HasCustomValidation((activator, target, level) =>
                {
                    return Validation(activator, target, level);
                })
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
        private static void Backstab2(AbilityBuilder builder)
        {
            builder.Create(FeatType.Backstab2, PerkType.Backstab)
                .Name("Backstab II")
                .HasRecastDelay(RecastGroup.Backstab, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .HasCustomValidation((activator, target, level) =>
                {
                    return Validation(activator, target, level);
                })
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
        private static void Backstab3(AbilityBuilder builder)
        {
            builder.Create(FeatType.Backstab3, PerkType.Backstab)
                .Name("Backstab III")
                .HasRecastDelay(RecastGroup.Backstab, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(8)
                .IsCastedAbility()
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