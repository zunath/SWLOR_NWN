//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class TranquilizerShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            TranquilizerShot1(builder);
            TranquilizerShot2(builder);
            TranquilizerShot3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.RifleBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a rifle ability.";
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
                    damage = d4(2);
                    inflict = true;
                    duration = 12f;

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
                    if (inflict) StatusEffect.Apply(activator, target, StatusEffectType.Tranquilize, duration);
                    break;
                case 2:
                    damage = d4(3);
                    inflict = true;
                    duration = 24f;

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
                    if (inflict) StatusEffect.Apply(activator, target, StatusEffectType.Tranquilize, duration);
                    break;
                case 3:
                    damage = d2(4);
                    inflict = true;
                    duration = 12f;

                    var count = 0;
                    var creature = GetFirstObjectInShape(Shape.Cone, RadiusSize.Colossal, GetLocation(target), true, ObjectType.Creature);
                    while (GetIsObjectValid(creature) && count < 3)
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                        if (inflict) StatusEffect.Apply(activator, target, StatusEffectType.Tranquilize, duration);
                        count++;

                        creature = GetNextObjectInShape(Shape.Cone, RadiusSize.Colossal, GetLocation(target), true, ObjectType.Creature);
                    }
                    break;
                default:
                    break;
            }

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void TranquilizerShot1(AbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot1, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot I")
                .HasRecastDelay(RecastGroup.TranquilizerShot, 30f)
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
        private static void TranquilizerShot2(AbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot2, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot II")
                .HasRecastDelay(RecastGroup.TranquilizerShot, 30f)
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
        private static void TranquilizerShot3(AbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot3, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot III")
                .HasRecastDelay(RecastGroup.TranquilizerShot, 30f)
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