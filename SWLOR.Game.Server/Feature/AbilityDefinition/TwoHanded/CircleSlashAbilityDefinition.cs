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
    public class CircleSlashAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            CircleSlash1(builder);
            CircleSlash2(builder);
            CircleSlash3(builder);

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
                    dmg = 2.0f;
                    break;
                case 2:
                    dmg = 4.5f;
                    break;
                case 3:
                    dmg = 6.5f;
                    break;
                default:
                    break;
            }

            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Small, GetLocation(activator), true);
            while (GetIsObjectValid(creature) && count < 3)
            {
                var willpower = GetAbilityModifier(AbilityType.Willpower, activator);
                var defense = Stat.GetDefense(target, CombatDamageType.Physical);
                var vitality = GetAbilityModifier(AbilityType.Vitality, target);
                var damage = Combat.CalculateDamage(dmg, willpower, defense, vitality, 0);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
                CombatPoint.AddCombatPoint(activator, creature, SkillType.TwoHanded, 3);

                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Small, GetLocation(activator), true);
                count++;
            }            

            Enmity.ModifyEnmityOnAll(activator, 1);
        }

        private static void CircleSlash1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CircleSlash1, PerkType.CircleSlash)
                .Name("Circle Slash I")
                .HasRecastDelay(RecastGroup.CircleSlash, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CircleSlash2(AbilityBuilder builder)
        {
            builder.Create(FeatType.CircleSlash2, PerkType.CircleSlash)
                .Name("Circle Slash II")
                .HasRecastDelay(RecastGroup.CircleSlash, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CircleSlash3(AbilityBuilder builder)
        {
            builder.Create(FeatType.CircleSlash3, PerkType.CircleSlash)
                .Name("Circle Slash III")
                .HasRecastDelay(RecastGroup.CircleSlash, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}