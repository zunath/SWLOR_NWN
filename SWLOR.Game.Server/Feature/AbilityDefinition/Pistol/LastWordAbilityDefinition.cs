using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class LastWordAbilityDefinition : IAbilityListDefinition
    {
        private const SkillType Skill = SkillType.Pistol;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            LastWord1(builder);

            return builder.Build();
        }

        private static void LastWord1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.LastWord1, PerkType.LastWord)
                .Name("Last Word")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(Skill)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.PointPistol)
                .IsAreaAbility()
                .HasImpactAction(LastWord1ImpactAction)
                .HasTargetingCone(
                    Spell.LastWord1,
                    5f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void LastWord1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                Skill,
                25,
                45,
                typeof(LastWordStatusEffect),
                CombatImpactAreaShape.Cone,
                0.25f,
                5f,
                5f,
                beforeImpact: affectedEnemy => AssignCommand(affectedEnemy, () => ClearAllActions()),
                afterSuccessfulHit: affectedEnemy =>
                    StatusEffect.ApplyStatusEffect(
                        activator,
                        affectedEnemy,
                        typeof(DazedStatusEffect),
                        3f,
                        CombatDamageType.Physical));
        }
    }
}
