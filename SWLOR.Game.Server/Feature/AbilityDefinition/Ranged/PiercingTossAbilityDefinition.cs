﻿//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class PiercingTossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            PiercingToss1(builder);
            PiercingToss2(builder);
            PiercingToss3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.ThrowingWeaponBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a throwing ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            var duration = 0f;
            var inflict = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 12;
                    if (d2() == 1) inflict = true;
                    duration = 30f;
                    break;
                case 2:
                    dmg = 21;
                    if (d4() > 1) inflict = true;
                    duration = 60f;
                    break;
                case 3:
                    dmg = 34;
                    inflict = true;
                    duration = 60f;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Ranged);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.Ranged);
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
            if (inflict) 
                StatusEffect.Apply(activator, target, StatusEffectType.Bleed, duration);

            CombatPoint.AddCombatPoint(activator, target, SkillType.Ranged, 3);
            Enmity.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private static void PiercingToss1(AbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingToss1, PerkType.PiercingToss)
                .Name("Piercing Toss I")
                .Level(1)
                .HasRecastDelay(RecastGroup.PiercingToss, 60f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(15.0f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .UsesAnimation(Animation.ThrowGrenade)
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void PiercingToss2(AbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingToss2, PerkType.PiercingToss)
                .Name("Piercing Toss II")
                .Level(2)
                .HasRecastDelay(RecastGroup.PiercingToss, 60f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(15.0f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .UsesAnimation(Animation.ThrowGrenade)
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void PiercingToss3(AbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingToss3, PerkType.PiercingToss)
                .Name("Piercing Toss III")
                .Level(3)
                .HasRecastDelay(RecastGroup.PiercingToss, 60f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(15.0f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .UsesAnimation(Animation.ThrowGrenade)
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}