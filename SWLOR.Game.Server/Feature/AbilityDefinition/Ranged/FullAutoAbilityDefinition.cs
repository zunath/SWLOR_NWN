//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class FullAutoAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            FullAuto1(builder);
            FullAuto2(builder);
            FullAuto3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.CannonBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a cannon ability.";
            }
            else 
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var damage = 0;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    damage = d8();
                    break;
                case 2:
                    damage = d6(2);
                    break;
                case 3:
                    damage = d6(3);
                    break;
                default:
                    break;
            }

            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Cone, RadiusSize.Colossal, GetLocation(target), true, ObjectType.Creature);
            while (GetIsObjectValid(creature) && count < 3)
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                count++;
                
                creature = GetNextObjectInShape(Shape.Cone, RadiusSize.Colossal, GetLocation(target), true, ObjectType.Creature);                
            }            

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void FullAuto1(AbilityBuilder builder)
        {
            builder.Create(FeatType.FullAuto1, PerkType.FullAuto)
                .Name("Full Auto I")
                .HasRecastDelay(RecastGroup.FullAuto, 30f)
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
        private static void FullAuto2(AbilityBuilder builder)
        {
            builder.Create(FeatType.FullAuto2, PerkType.FullAuto)
                .Name("Full Auto II")
                .HasRecastDelay(RecastGroup.FullAuto, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(4)
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
        private static void FullAuto3(AbilityBuilder builder)
        {
            builder.Create(FeatType.FullAuto3, PerkType.FullAuto)
                .Name("Full Auto III")
                .HasRecastDelay(RecastGroup.FullAuto, 30f)
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
    }
}