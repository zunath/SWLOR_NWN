//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class CrossCutAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            CrossCut1(builder);
            CrossCut2(builder);
            CrossCut3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.TwinBladeBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a twin-blade ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0.0f;
            var acLoss = 0;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 4.0f;
                    acLoss = 2;
                    break;
                case 2:
                    dmg = 9.0f;
                    acLoss = 4;
                    break;
                case 3:
                    dmg = 14.0f;
                    acLoss = 6;
                    break;
                default:
                    break;
            }

            var might = GetAbilityModifier(AbilityType.Might, activator);
            var defense = Combat.CalculateDefense(target);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = Combat.CalculateDamage(dmg, might, defense, vitality, false);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
            ApplyEffectToObject(DurationType.Temporary, EffectACDecrease(acLoss), target, 60f);
            CombatPoint.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
        }

        private static void CrossCut1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrossCut1, PerkType.CrossCut)
                .Name("Cross Cut I")
                .HasRecastDelay(RecastGroup.CrossCut, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private static void CrossCut2(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrossCut2, PerkType.CrossCut)
                .Name("Cross Cut II")
                .HasRecastDelay(RecastGroup.CrossCut, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private static void CrossCut3(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrossCut3, PerkType.CrossCut)
                .Name("Cross Cut III")
                .HasRecastDelay(RecastGroup.CrossCut, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
    }
}