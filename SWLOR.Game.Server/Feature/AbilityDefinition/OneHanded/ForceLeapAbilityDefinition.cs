﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
            var rightHandType = GetBaseItemType(weapon);

            if (!Item.LightsaberBaseItemTypes.Contains(rightHandType))
            {
                return "A lightsaber must be equipped in your right hand to use this ability.";
            }

            if (GetDistanceBetween(activator, target) < 8)
            {
                return "You must get further away from the target to use this ability.";
            }

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
                    dmg = 15;
                    break;
                case 3:
                    dmg = 23;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            const float Delay = 1.2f;
            ClearAllActions();
            AssignCommand(activator, () =>
            {
                PlaySound("plr_force_flip");
                ActionPlayAnimation(Animation.ForceLeap, 2.0f, 1.0f);
            });

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);

            var attackerStat = GetAbilityScore(activator, AbilityType.Might);
            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.OneHanded);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var rightHandBaseItemType = GetBaseItemType(weapon);

            DelayCommand(Delay, () =>
            {                
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Sonic), target);
                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, 2f);
                AssignCommand(activator, () =>
                {
                    if (Item.LightsaberBaseItemTypes.Contains(rightHandBaseItemType))
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
        }

        private static void ForceLeap1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLeap1, PerkType.ForceLeap)
                .Name("Force Leap I")
                .HasRecastDelay(RecastGroup.ForceLeap, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .HasMaxRange(20f)
                .IsCastedAbility()
                .IsHostileAbility()
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
                .HasActivationDelay(0.5f)
                .HasMaxRange(20f)
                .IsCastedAbility()
                .IsHostileAbility()
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
                .HasActivationDelay(0.5f)
                .HasMaxRange(20f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}