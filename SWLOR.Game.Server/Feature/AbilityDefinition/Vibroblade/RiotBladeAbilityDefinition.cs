using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class RiotBladeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureRiotBlade(
                builder
                    .Create(FeatType.RiotBlade1, PerkType.RiotBlade)
                    .Name("Riot Blade I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.RiotBlade, 30f),
                15,
                3);
            ConfigureRiotBlade(
                builder
                    .Create(FeatType.RiotBlade2, PerkType.RiotBlade)
                    .Name("Riot Blade II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.RiotBlade, 30f),
                30,
                5);
            ConfigureRiotBlade(
                builder
                    .Create(FeatType.RiotBlade3, PerkType.RiotBlade)
                    .Name("Riot Blade III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.RiotBlade, 30f),
                45,
                8);

            return builder.Build();
        }

        private static void ConfigureRiotBlade(AbilityBuilder ability, int baseDamage, int stamina)
        {
            ability.HasActivationDelay(0f)
                .UsesAnimation(Animation.RiotBlade)
                .SkillType(SkillType.Vibroblade)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        SkillType.Vibroblade,
                        baseDamage,
                        0,
                        null,
                        false);

                    ApplySecondaryDamage(activator, target, targetLocation);
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void ApplySecondaryDamage(uint activator, uint primaryTarget, Location targetLocation)
        {
            const float Radius = 5f;

            var bonus = Stat.GetStatAdjustment(activator, StatType.RiotBladeSecondaryDamageBonus);
            if (bonus <= 0)
                return;

            var center = GetIsObjectValid(primaryTarget) ? GetLocation(primaryTarget) : targetLocation;
            var creature = GetFirstObjectInShape(Shape.Sphere, Radius, center, true);
            while (GetIsObjectValid(creature))
            {
                if (creature != primaryTarget && GetIsReactionTypeHostile(creature, activator))
                {
                    Ability.ApplyCombatImpact(
                        activator,
                        creature,
                        GetLocation(creature),
                        SkillType.Vibroblade,
                        bonus,
                        0,
                        null,
                        false,
                        playImpactAnimation: false);
                }

                creature = GetNextObjectInShape(Shape.Sphere, Radius, center, true);
            }
        }
    }
}
