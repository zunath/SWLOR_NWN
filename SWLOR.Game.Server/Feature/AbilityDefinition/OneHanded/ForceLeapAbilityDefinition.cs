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
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded
{
    public class ForceLeapAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceLeap1(builder);
            ForceLeap2(builder);
            ForceLeap3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var offHand = GetItemInSlot(InventorySlot.LeftHand, activator);
            var rightHandBaseItemType = GetBaseItemType(weapon);
            var leftHandBaseItemType = GetBaseItemType(offHand);
            
            if (rightHandBaseItemType == BaseItem.Lightsaber
                && (leftHandBaseItemType == BaseItem.SmallShield ||
                    leftHandBaseItemType == BaseItem.LargeShield ||
                    leftHandBaseItemType == BaseItem.TowerShield ||
                    leftHandBaseItemType == BaseItem.Invalid))
            {
                return "This is a one-handed ability.";
            }

            if (GetDistanceBetween(activator, target) < 8)
            {
                return "You must get further away from the target to use this ability.";
            }

            return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0.0f;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 1.5f;
                    break;
                case 2:
                    dmg = 4.0f;
                    break;
                case 3:
                    dmg = 6.5f;
                    break;
                default:
                    break;
            }

            const float Delay = 1.2f;
            ClearAllActions();
            AssignCommand(activator, () =>
            {
                PlaySound("plr_force_flip");
                ActionPlayAnimation(Animation.ForceLeap, 2.0f, 1.0f);
            });

            var willpower = GetAbilityModifier(AbilityType.Willpower, activator);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = Combat.CalculateDamage(dmg, willpower, defense, vitality, 0);
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var rightHandBaseItemType = GetBaseItemType(weapon);

            DelayCommand(Delay, () =>
            {                
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Sonic), target);
                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, 2f);
                AssignCommand(activator, () =>
                {
                    if (rightHandBaseItemType == BaseItem.Lightsaber)
                    {
                        PlaySound("cb_ht_saberchan1");
                    }
                    else
                    {
                        PlaySound("cb_ht_critical");
                    }
                    ActionJumpToObject(target);
                });
            });

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void ForceLeap1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLeap1, PerkType.ForceLeap)
                .Name("Force Leap I")
                .HasRecastDelay(RecastGroup.ForceLeap, 30f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ForceLeap2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLeap2, PerkType.ForceLeap)
                .Name("Force Leap II")
                .HasRecastDelay(RecastGroup.ForceLeap, 30f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ForceLeap3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLeap3, PerkType.ForceLeap)
                .Name("Force Leap III")
                .HasRecastDelay(RecastGroup.ForceLeap, 30f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}