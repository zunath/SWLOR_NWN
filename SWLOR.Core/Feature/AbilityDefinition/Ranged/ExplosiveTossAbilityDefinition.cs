//using Random = SWLOR.Game.Server.Service.Random;

using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.CombatService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.SkillService;

namespace SWLOR.Core.Feature.AbilityDefinition.Ranged
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

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.ThrowingWeaponBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a throwing ability.";
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
                    dmg = 8;
                    break;
                case 2:
                    dmg = 16;
                    break;
                case 3:
                    dmg = 26;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Ranged);

            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.Ranged);
            var attackerStat = Combat.GetPerkAdjustedAbilityScore(activator);
            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Medium, GetLocation(target), true, ObjectType.Creature);
            while (GetIsObjectValid(creature) && count < 3)
            {
                if (GetDistanceBetween(target, creature) <= 3f)
                {

                    var defense = Stat.GetDefense(creature, CombatDamageType.Physical, AbilityType.Vitality);
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
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), dTarget);
                    });

                    CombatPoint.AddCombatPoint(activator, creature, SkillType.Ranged, 3);
                    Enmity.ModifyEnmity(activator, creature, 250 * level + damage);

                    count++;
                }
                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Medium, GetLocation(target), true, ObjectType.Creature);
            }
        }

        private static void ExplosiveToss1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ExplosiveToss1, PerkType.ExplosiveToss)
                .Name("Explosive Toss I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ExplosiveToss, 30f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(15.0f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .UsesAnimation(Animation.ThrowGrenade)
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ExplosiveToss2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ExplosiveToss2, PerkType.ExplosiveToss)
                .Name("Explosive Toss II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ExplosiveToss, 30f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(15.0f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .UsesAnimation(Animation.ThrowGrenade)
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ExplosiveToss3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ExplosiveToss3, PerkType.ExplosiveToss)
                .Name("Explosive Toss III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ExplosiveToss, 30f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(15.0f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .UsesAnimation(Animation.ThrowGrenade)
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}