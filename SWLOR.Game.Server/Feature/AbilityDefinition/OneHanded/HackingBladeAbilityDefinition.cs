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
            var rightHandType = GetBaseItemType(weapon);

            if (Item.VibrobladeBaseItemTypes.Contains(rightHandType))
            {
                return string.Empty;

            }
            else
                return "A vibroblade must be equipped in your right hand to use this ability.";
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            int dmg;
            int dc;

            switch (level)
            {
                default:
                case 1:
                    dmg = 6;
                    dc = 10;
                    break;
                case 2:
                    dmg = 15;
                    dc = 15;
                    break;
                case 3:
                    dmg = 22;
                    dc = 20;
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            CombatPoint.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

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
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

            if (checkResult == SavingThrowResultType.Failed)
            {
                StatusEffect.Apply(activator, target, StatusEffectType.Bleed, 60f);
            }
            
            Enmity.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private static void HackingBlade1(AbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade1, PerkType.HackingBlade)
                .Name("Hacking Blade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.HackingBlade, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void HackingBlade2(AbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade2, PerkType.HackingBlade)
                .Name("Hacking Blade II")
                .Level(2)
                .HasRecastDelay(RecastGroup.HackingBlade, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void HackingBlade3(AbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade3, PerkType.HackingBlade)
                .Name("Hacking Blade III")
                .Level(3)
                .HasRecastDelay(RecastGroup.HackingBlade, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}