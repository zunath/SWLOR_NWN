//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class CrescentMoonAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            CrescentMoon1(builder);
            CrescentMoon2(builder);
            CrescentMoon3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.HeavyVibrobladeBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a heavy vibroblade ability.";
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
                    dmg = 12;
                    break;
                case 2:
                    dmg = 21;
                    break;
                case 3:
                    dmg = 34;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.TwoHanded);

            var attackerStat = GetAbilityScore(activator, AbilityType.Might);
            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.TwoHanded);
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
            ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, 3f);

            CombatPoint.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
            Enmity.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private static void CrescentMoon1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon1, PerkType.CrescentMoon)
                .Name("Crescent Moon I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CrescentMoon2(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon2, PerkType.CrescentMoon)
                .Name("Crescent Moon II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CrescentMoon3(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon3, PerkType.CrescentMoon)
                .Name("Crescent Moon III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}