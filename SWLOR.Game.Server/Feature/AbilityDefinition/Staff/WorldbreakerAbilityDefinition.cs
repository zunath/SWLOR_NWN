using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class WorldbreakerAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Worldbreaker1(builder);

            return builder.Build();
        }

        private static void Worldbreaker1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Worldbreaker1, PerkType.Worldbreaker)
                .Name("Worldbreaker")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Staff)
                .IsAreaAbility()
                .HasImpactAction(Worldbreaker1ImpactAction)
                .HasTargetingSphere(
                    Spell.Worldbreaker1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void Worldbreaker1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Staff,
                25,
                45,
                typeof(WorldbreakerStatusEffect),
                CombatImpactAreaShape.Sphere,
                0.25f,
                5f,
                centerOnActivator: true,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Screen_Shake,
                afterSuccessfulHit: affectedEnemy =>
                    StatusEffect.ApplyStatusEffect(
                        activator,
                        affectedEnemy,
                        typeof(KnockdownStatusEffect),
                        3f,
                        CombatDamageType.Physical));
        }
    }
}
