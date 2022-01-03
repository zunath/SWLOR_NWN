//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class DoubleStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            DoubleStrike1(builder);
            DoubleStrike2(builder);
            DoubleStrike3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.SaberstaffBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a saberstaff ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0.0f;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 1.5f;
                    break;
                case 2:
                    dmg = 4.0f;
                    break;
                case 3:
                    dmg = 6.0f;
                    break;
                default:
                    break;
            }

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);

            var willpower = GetAbilityModifier(AbilityType.Willpower, activator);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = Combat.CalculateDamage(dmg, willpower, defense, vitality, 0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Sonic), target);
        }

        private static void DoubleStrike1(AbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleStrike1, PerkType.DoubleStrike)
                .Name("Double Strike I")
                .HasRecastDelay(RecastGroup.DoubleStrike, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private static void DoubleStrike2(AbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleStrike2, PerkType.DoubleStrike)
                .Name("Double Strike II")
                .HasRecastDelay(RecastGroup.DoubleStrike, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private static void DoubleStrike3(AbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleStrike3, PerkType.DoubleStrike)
                .Name("Double Strike III")
                .HasRecastDelay(RecastGroup.DoubleStrike, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
    }
}