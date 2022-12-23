//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
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

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.TwinBladeBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a twin blade ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 10;
                    break;
                case 2:
                    dmg = 18;
                    break;
                case 3:
                    dmg = 28;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.TwoHanded);

            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Large, GetLocation(activator), true, ObjectType.Creature);
            while (GetIsObjectValid(creature) && count < 3)
            {
                if(GetIsReactionTypeHostile(creature, activator))
                {
                    var attackerStat = GetAbilityScore(activator, AbilityType.Might);
                    var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.TwoHanded);
                    var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(creature, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var dTarget = creature;

                    DelayCommand(0.1f, () =>
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), dTarget));

                    CombatPoint.AddCombatPoint(activator, creature, SkillType.TwoHanded, 3);
                    Enmity.ModifyEnmity(activator, creature, 250 * level + damage);
                    count++;
                }
                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Large, GetLocation(activator), true, ObjectType.Creature);
            }

            AssignCommand(activator, () => ActionPlayAnimation(Animation.Whirlwind));
        }

        private static void SpinningWhirl1(AbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningWhirl1, PerkType.SpinningWhirl)
                .Name("Spinning Whirl I")
                .Level(1)
                .HasRecastDelay(RecastGroup.SpinningWhirl, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void SpinningWhirl2(AbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningWhirl2, PerkType.SpinningWhirl)
                .Name("Spinning Whirl II")
                .Level(2)
                .HasRecastDelay(RecastGroup.SpinningWhirl, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void SpinningWhirl3(AbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningWhirl3, PerkType.SpinningWhirl)
                .Name("Spinning Whirl III")
                .Level(3)
                .HasRecastDelay(RecastGroup.SpinningWhirl, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}