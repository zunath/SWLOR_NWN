//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

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
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            var enmity = level * 300;
            switch (level)
            {
                case 1:
                    Enmity.ModifyEnmity(activator, target, enmity);
                    StatusEffect.Apply(activator, target, StatusEffectType.Tranquilize, 12f);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Ranged, 3);
                    break;
                case 2:
                    Enmity.ModifyEnmity(activator, target, enmity);
                    StatusEffect.Apply(activator, target, StatusEffectType.Tranquilize, 24f);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Ranged, 3);
                    break;
                case 3:
                    var count = 0;
                    var creature = GetFirstObjectInShape(Shape.SpellCone, RadiusSize.Colossal, GetLocation(target), true, ObjectType.Creature);
                    while (GetIsObjectValid(creature) && count < 3)
                    {
                        if(creature == activator) {
                            creature = GetNextObjectInShape(Shape.SpellCone, RadiusSize.Colossal, GetLocation(target), true, ObjectType.Creature);
                            continue;
                        }
                        Enmity.ModifyEnmity(activator, creature, enmity);
                        StatusEffect.Apply(activator, creature, StatusEffectType.Tranquilize, 12f);
                        CombatPoint.AddCombatPoint(activator, creature, SkillType.Ranged, 3);
                        count++;

                        creature = GetNextObjectInShape(Shape.SpellCone, RadiusSize.Colossal, GetLocation(target), true, ObjectType.Creature);
                    }
                    break;
                default:
                    break;
            }

        }

        private static void TranquilizerShot1(AbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot1, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot I")
                .Level(1)
                .HasRecastDelay(RecastGroup.TranquilizerShot, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void TranquilizerShot2(AbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot2, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot II")
                .Level(2)
                .HasRecastDelay(RecastGroup.TranquilizerShot, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void TranquilizerShot3(AbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot3, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot III")
                .Level(3)
                .HasRecastDelay(RecastGroup.TranquilizerShot, 300f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}