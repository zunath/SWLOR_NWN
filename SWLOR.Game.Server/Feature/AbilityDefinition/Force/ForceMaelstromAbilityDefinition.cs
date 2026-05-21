using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ForceMaelstromAbilityDefinition : IAbilityListDefinition
    {
        private const float PullDistance = 2f;
        private const float MinimumDistanceAfterPull = 1.5f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceMaelstrom1(builder);

            return builder.Build();
        }

        private static void ForceMaelstrom1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceMaelstrom1, PerkType.ForceMaelstrom)
                .Name("Force Maelstrom")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ForceMaelstrom, 75f)
                .SkillType(SkillType.Force)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(ForceMaelstrom1ImpactAction)
                .HasTargetingSphere(
                    Spell.ForceMaelstrom1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void ForceMaelstrom1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                10,
                12,
                null,
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind,
                afterSuccessfulHit: hitTarget => PullTowardActivator(activator, hitTarget));
        }

        private static void PullTowardActivator(uint activator, uint target)
        {
            if (!GetIsObjectValid(activator) ||
                !GetIsObjectValid(target) ||
                GetArea(activator) != GetArea(target))
            {
                return;
            }

            var targetPosition = GetPosition(target);
            var pullVector = GetPosition(activator) - targetPosition;
            var distance = pullVector.Length();
            if (distance <= MinimumDistanceAfterPull)
                return;

            var pullDistance = Math.Min(PullDistance, distance - MinimumDistanceAfterPull);
            var destination = targetPosition + pullVector / distance * pullDistance;
            var pullLocation = Location(GetArea(target), destination, GetFacing(target));
            AssignCommand(target, () => JumpToLocation(pullLocation));
        }

    }
}
