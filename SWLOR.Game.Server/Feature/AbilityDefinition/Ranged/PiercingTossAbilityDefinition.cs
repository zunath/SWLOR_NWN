//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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

        private static string Validation(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.ThrowingWeaponBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a throwing ability.";
            }
            else 
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var damage = 0;
            var duration = 0f;
            var inflict = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    damage = d4();
                    if (d2() == 1) inflict = true;
                    duration = 30f;
                    break;
                case 2:
                    damage = d4(2);
                    if (d4() > 1) inflict = true;
                    duration = 60f;
                    break;
                case 3:
                    damage = d4(4);
                    inflict = true;
                    duration = 60f;
                    break;
                default:
                    break;
            }

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
            if (inflict) StatusEffect.Apply(activator, target, StatusEffectType.Bleed, duration);

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void PiercingToss1(AbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingToss1, PerkType.PiercingToss)
                .Name("Piercing Toss I")
                .HasRecastDelay(RecastGroup.PiercingToss, 60f)
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
        private static void PiercingToss2(AbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingToss2, PerkType.PiercingToss)
                .Name("Piercing Toss II")
                .HasRecastDelay(RecastGroup.PiercingToss, 60f)
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
        private static void PiercingToss3(AbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingToss3, PerkType.PiercingToss)
                .Name("Piercing Toss III")
                .HasRecastDelay(RecastGroup.PiercingToss, 60f)
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