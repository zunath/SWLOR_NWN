//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

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
            var dmg = 0;


            switch (level)
            {
                case 1:
                    dmg = 14;
                    break;
                case 2:
                    dmg = 24;
                    break;
                case 3:
                    dmg = 32;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.TwoHanded);

            var stat = AbilityType.Perception;
            if (Ability.IsAbilityToggled(activator, AbilityToggleType.StrongStyleSaberstaff))
            {
                stat = AbilityType.Might;
            }

            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Large, GetLocation(activator), true);
            while (GetIsObjectValid(creature) && count < 6)
            {
                if (GetIsReactionTypeHostile(creature, activator))
                {
                    var attackerStat = Combat.GetPerkAdjustedAbilityScore(activator);
                    var attack = Stat.GetAttack(activator, stat, SkillType.TwoHanded);
                    var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
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
                    Enmity.ModifyEnmity(activator, creature, 100 * level + damage);
                    count++;
                }

                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Large, GetLocation(activator), true);
            }

            AssignCommand(activator, () => ActionPlayAnimation(Animation.Whirlwind));
        }

        private static void CircleSlash1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CircleSlash1, PerkType.CircleSlash)
                .Name("Circle Slash I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CircleSlash, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CircleSlash2(AbilityBuilder builder)
        {
            builder.Create(FeatType.CircleSlash2, PerkType.CircleSlash)
                .Name("Circle Slash II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CircleSlash, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CircleSlash3(AbilityBuilder builder)
        {
            builder.Create(FeatType.CircleSlash3, PerkType.CircleSlash)
                .Name("Circle Slash III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CircleSlash, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}