//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class SpinningWhirlAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            SpinningWhirl1(builder);
            SpinningWhirl2(builder);
            SpinningWhirl3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.StaffBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a staff ability.";
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
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Small, GetLocation(activator), true, ObjectType.Creature);
            while (GetIsObjectValid(creature) && count < 3)
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Bludgeoning), target);

                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Small, GetLocation(activator), true, ObjectType.Creature);
                count++;
            }            

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.MartialArts, 3);
        }

        private static void SpinningWhirl1(AbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningWhirl1, PerkType.SpinningWhirl)
                .Name("Spinning Whirl I")
                .HasRecastDelay(RecastGroup.SpinningWhirl, 30f)
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
        private static void SpinningWhirl2(AbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningWhirl2, PerkType.SpinningWhirl)
                .Name("Spinning Whirl II")
                .HasRecastDelay(RecastGroup.SpinningWhirl, 30f)
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
        private static void SpinningWhirl3(AbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningWhirl3, PerkType.SpinningWhirl)
                .Name("Spinning Whirl III")
                .HasRecastDelay(RecastGroup.SpinningWhirl, 30f)
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