//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
            var dmg = 0.0f;
            var inflict = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 7.0f;
                    inflict = true;
                    break;
                case 2:
                    dmg = 8.5f;
                    inflict = true;
                    break;
                case 3:
                    dmg = 12.0f;
                    inflict = true;
                    break;
                default:
                    break;
            }

            CombatPoint.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);

            var might = GetAbilityModifier(AbilityType.Might, activator);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = Combat.CalculateDamage(dmg, might, defense, vitality, 0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
            if (inflict) ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, 3f);
        }

        private static void CrescentMoon1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon1, PerkType.CrescentMoon)
                .Name("Crescent Moon I")
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .IsHostileAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CrescentMoon2(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon2, PerkType.CrescentMoon)
                .Name("Crescent Moon II")
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .IsHostileAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CrescentMoon3(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon3, PerkType.CrescentMoon)
                .Name("Crescent Moon III")
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .IsHostileAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}