using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class StasisVolleyAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            StasisVolley1(builder);

            return builder.Build();
        }

        private static void StasisVolley1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.StasisVolley1, PerkType.StasisVolley)
                .Name("Stasis Volley")
                .Level(1)
                .HasActivationDelay(2f)
                .SkillType(SkillType.Rifle)
                .UsesImpactAnimation(Animation.PointPistol)
                .IsAreaAbility()
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .HasImpactAction(StasisVolley1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void StasisVolley1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Rifle,
                20,
                45,
                typeof(StasisVolleyStatusEffect),
                CombatImpactAreaShape.Cone,
                0.25f,
                5f,
                5f,
                afterSuccessfulHit: affectedEnemy =>
                    StatusEffect.ApplyStatusEffect(
                        activator,
                        affectedEnemy,
                        typeof(TranquilizedStatusEffect),
                        3f,
                        CombatDamageType.Physical));
        }
    }
}
