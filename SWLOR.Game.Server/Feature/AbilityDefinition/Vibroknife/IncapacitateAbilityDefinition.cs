using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class IncapacitateAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const float IncapacitateRadius = 5f;
        private const float IncapacitateDurationSeconds = 20f;
        private const float CascadeFailureTelegraphSeconds = 0.25f;
        private const float CascadeFailureConeLength = 5f;
        private const float CascadeFailureConeWidth = 5f;
        private const float CascadeFailureVulnerableDurationSeconds = 12f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.Incapacitate1, PerkType.Incapacitate)
                .Name("Incapacitate")
                .Level(1)
                .HasTargetingSphere(
                    Spell.Incapacitate1,
                    IncapacitateRadius,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.Incapacitate, 120f)
                .UsesAnimation(Animation.Backstab)
                .HasImpactAction(Incapacitate1ImpactAction)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);

            return builder.Build();
        }

        private static void Incapacitate1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyStatusToNearbyEnemies(
                activator,
                target,
                targetLocation,
                typeof(IncapacitateStatusEffect),
                IncapacitateDurationSeconds,
                true,
                0);

            if (Stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurCascadeFailure) <= 0)
                return;

            Telegraph.CreateConeTelegraph(
                activator,
                GetPosition(activator),
                GetImpactRotationRadians(activator, target, targetLocation),
                CascadeFailureConeLength,
                CascadeFailureConeWidth,
                CascadeFailureTelegraphSeconds,
                true,
                ApplyCascadeFailure);
        }

        private static void ApplyCascadeFailure(uint activator, List<uint> affectedCreatures)
        {
            if (!GetIsObjectValid(activator) || GetCurrentHitPoints(activator) <= 0)
                return;

            foreach (var creature in affectedCreatures.Distinct())
            {
                if (!GetIsObjectValid(creature) || !GetIsReactionTypeHostile(creature, activator))
                    continue;

                StatusEffect.ApplyStatusEffect(
                    activator,
                    creature,
                    typeof(VulnerableStatusEffect),
                    CascadeFailureVulnerableDurationSeconds,
                    CombatDamageType.Physical);
            }
        }

        private static float GetImpactRotationRadians(uint activator, uint target, Location targetLocation)
        {
            var origin = GetPosition(activator);
            var destination = GetIsObjectValid(target)
                ? GetPosition(target)
                : GetIsObjectValid(GetAreaFromLocation(targetLocation))
                    ? GetPositionFromLocation(targetLocation)
                    : origin;
            var delta = destination - origin;

            if (Math.Abs(delta.X) <= 0.01f && Math.Abs(delta.Y) <= 0.01f)
                return GetFacing(activator) * ((float)Math.PI / 180f);

            return (float)Math.Atan2(delta.Y, delta.X);
        }
    }
}
