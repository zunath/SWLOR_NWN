﻿//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
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

            switch (level)
            {
                case 1:
                    Enmity.ModifyEnmity(activator, target, 30);
                    StatusEffect.Apply(activator, target, StatusEffectType.Tranquilize, 12f);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Ranged, 3);
                    break;
                case 2:
                    Enmity.ModifyEnmity(activator, target, 60);
                    StatusEffect.Apply(activator, target, StatusEffectType.Tranquilize, 24f);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Ranged, 3);
                    break;
                case 3:
                    var count = 0;
                    var creature = GetFirstObjectInShape(Shape.Cone, RadiusSize.Colossal, GetLocation(target), true, ObjectType.Creature);
                    while (GetIsObjectValid(creature) && count < 3)
                    {

                        Enmity.ModifyEnmity(activator, creature, 30);
                        StatusEffect.Apply(activator, creature, StatusEffectType.Tranquilize, 12f);
                        CombatPoint.AddCombatPoint(activator, creature, SkillType.Ranged, 3);
                        count++;

                        creature = GetNextObjectInShape(Shape.Cone, RadiusSize.Colossal, GetLocation(target), true, ObjectType.Creature);
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
                .HasRecastDelay(RecastGroup.TranquilizerShot, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void TranquilizerShot2(AbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot2, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot II")
                .HasRecastDelay(RecastGroup.TranquilizerShot, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void TranquilizerShot3(AbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot3, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot III")
                .HasRecastDelay(RecastGroup.TranquilizerShot, 300f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}