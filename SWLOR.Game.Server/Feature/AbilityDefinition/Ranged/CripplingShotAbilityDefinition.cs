//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class CripplingShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            CripplingShot1(builder);
            CripplingShot2(builder);
            CripplingShot3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.RifleBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a rifle ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0.0f;
            var duration = 0f;
            var inflict = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 7.0f;
                    duration = 30f;
                    if (d2() == 1) inflict = true;
                    break;
                case 2:
                    dmg = 8.5f;
                    duration = 60f;
                    if (d4() > 1) inflict = true;
                    break;
                case 3:
                    dmg = 12.0f;
                    duration = 60f;
                    inflict = true;
                    break;
                default:
                    break;
            }

            var perception = GetAbilityModifier(AbilityType.Perception, activator);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = Combat.CalculateDamage(dmg, perception, defense, vitality, false);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
            if (inflict) ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, duration);

            CombatPoint.AddCombatPoint(activator, target, SkillType.Ranged, 3);
        }

        private static void CripplingShot1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot1, PerkType.CripplingShot)
                .Name("Crippling Shot I")
                .HasRecastDelay(RecastGroup.CripplingShot, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CripplingShot2(AbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot2, PerkType.CripplingShot)
                .Name("Crippling Shot II")
                .HasRecastDelay(RecastGroup.CripplingShot, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CripplingShot3(AbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot3, PerkType.CripplingShot)
                .Name("Crippling Shot III")
                .HasRecastDelay(RecastGroup.CripplingShot, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}