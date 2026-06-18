using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class RainOfSteelAbilityDefinition : IAbilityListDefinition
    {
        private const float Radius = 8f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RainOfSteel1(builder);

            return builder.Build();
        }

        private static void RainOfSteel1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RainOfSteel1, PerkType.RainOfSteel)
                .Name("Rain of Steel")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Throwing)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .HasMaxRange(ThrowingAbilityRange.Standard)
                .IsAreaAbility()
                .HasImpactAction(RainOfSteel1ImpactAction)
                .HasTargetingSphere(
                    Spell.RainOfSteel1,
                    8f,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void RainOfSteel1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Throwing,
                25,
                45,
                typeof(BleedStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                Radius,
                targetVisualEffect: VisualEffect.Vfx_Com_Blood_Spark_Medium,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Swinging_Blade);
        }
    }
}
