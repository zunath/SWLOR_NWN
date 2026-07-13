using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class CoveringStrikeAbilityDefinition : IAbilityListDefinition
    {
        private const string ReplacementAnimationName = "Covering_Strike";
        private const float Radius = 5f;
        private const int DurationSeconds = 30;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureCoveringStrike(builder, FeatType.CoveringStrike1, Spell.CoveringStrike1, "Covering Strike I", 1, 15, 6);
            ConfigureCoveringStrike(builder, FeatType.CoveringStrike2, Spell.CoveringStrike2, "Covering Strike II", 2, 25, 6);
            ConfigureCoveringStrike(builder, FeatType.CoveringStrike3, Spell.CoveringStrike3, "Covering Strike III", 3, 30, 8);

            return builder.Build();
        }

        private static void ConfigureCoveringStrike(
            AbilityBuilder builder,
            FeatType featType,
            Spell spell,
            string name,
            int level,
            int baseDamage,
            int stamina)
        {
            builder
                .Create(featType, PerkType.CoveringStrike)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .UsesImpactAnimationOverwrite(ReplacementAnimationName)
                .HasRecastDelay(RecastGroup.CoveringStrike, 30f)
                .SkillType(SkillType.Vibroblade)
                .HasImpactAction((activator, target, effectivePerkLevel, targetLocation) =>
                    ApplyCoveringStrike(activator, target, targetLocation, baseDamage))
                .HasTargetingSphere(
                    spell,
                    Radius,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void ApplyCoveringStrike(uint activator, uint target, Location targetLocation, int baseDamage)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Vibroblade,
                baseDamage,
                DurationSeconds,
                typeof(CoveringStrikeStatusEffect),
                CombatImpactAreaShape.Sphere,
                0.25f,
                Radius,
                0f,
                centerOnActivator: true);
        }
    }
}
