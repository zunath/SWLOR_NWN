//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class SkewerAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            Skewer1(builder);
            Skewer2(builder);
            Skewer3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.PolearmBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a polearm ability.";
            }
            else 
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var damage = 0;
            var inflict = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    damage = d6();
                    if (Random(100)<45) inflict = true;
                    break;
                case 2:
                    damage = d6(2);
                    if (d4()>1) inflict = true;
                    break;
                case 3:
                    damage = d6(3);
                    inflict = true;
                    break;
                default:
                    break;
            }

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
            if (inflict)
            {
                Ability.EndConcentrationAbility(target);
                SendMessageToPC(activator, ColorToken.Gray(GetName(target)) + "'s  concentration has been broken.");
                SendMessageToPC(target, ColorToken.Gray(GetName(activator)) + " broke your concentration.");
            }

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.TwoHanded, 3);
        }

        private static void Skewer1(AbilityBuilder builder)
        {
            builder.Create(FeatType.Skewer1, PerkType.Skewer)
                .Name("Skewer I")
                .HasRecastDelay(RecastGroup.Skewer, 30f)
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
        private static void Skewer2(AbilityBuilder builder)
        {
            builder.Create(FeatType.Skewer2, PerkType.Skewer)
                .Name("Skewer II")
                .HasRecastDelay(RecastGroup.Skewer, 30f)
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
        private static void Skewer3(AbilityBuilder builder)
        {
            builder.Create(FeatType.Skewer3, PerkType.Skewer)
                .Name("Skewer III")
                .HasRecastDelay(RecastGroup.Skewer, 30f)
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