//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class ExplosiveTossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ExplosiveToss1(builder);
            ExplosiveToss2(builder);
            ExplosiveToss3(builder);

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
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    damage = d4();
                    break;
                case 2:
                    damage = d6();
                    break;
                case 3:
                    damage = d8();
                    break;
                default:
                    break;
            }

            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Medium, GetLocation(target), true, ObjectType.Creature);
            while (GetIsObjectValid(creature) && count < 3)
            {
                if (GetDistanceBetween(target, creature) <= 3f)
                {
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
                    count++;
                }
                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Medium, GetLocation(target), true, ObjectType.Creature);                
            }            

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void ExplosiveToss1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ExplosiveToss1, PerkType.ExplosiveToss)
                .Name("Explosive Toss I")
                .HasRecastDelay(RecastGroup.ExplosiveToss, 30f)
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
        private static void ExplosiveToss2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ExplosiveToss2, PerkType.ExplosiveToss)
                .Name("Explosive Toss II")
                .HasRecastDelay(RecastGroup.ExplosiveToss, 30f)
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
        private static void ExplosiveToss3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ExplosiveToss3, PerkType.ExplosiveToss)
                .Name("Explosive Toss III")
                .HasRecastDelay(RecastGroup.ExplosiveToss, 30f)
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