using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class PunishingStrikeAbilityDefinition : IAbilityListDefinition
    {
        private const VisualEffect AreaVisualEffect = VisualEffect.Vfx_Fnf_Swinging_Blade;
        private const VisualEffect TargetVisualEffect = VisualEffect.Vfx_Com_Blood_Spark_Medium;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PunishingStrike1(builder);

            return builder.Build();
        }

        private static void PunishingStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PunishingStrike1, PerkType.PunishingStrike)
                .Name("Punishing Strike")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleStrike)
                .HasRecastDelay(RecastGroup.PunishingStrike, 90f)
                .HasImpactAction(PunishingStrike1ImpactAction)
                .HasTargetingSphere(
                    Spell.PunishingStrike1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void PunishingStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Lightsaber,
                20,
                0,
                null,
                CombatImpactAreaShape.Sphere,
                0.25f,
                5f,
                centerOnActivator: true,
                targetVisualEffect: TargetVisualEffect,
                areaVisualEffect: AreaVisualEffect);
        }
    }
}
