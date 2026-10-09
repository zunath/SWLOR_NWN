using System;
using System.Collections.Generic;
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
    public sealed class RadiantLanceAbilityDefinition : IAbilityListDefinition
    {
        private const float LineLengthMeters = 8f;
        private const float LineWidthMeters = 2.5f;
        private const int Rank1BaseDamage = 16;
        private const int Rank2BaseDamage = 30;
        private const int Rank3BaseDamage = 44;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RadiantLance1(builder);
            RadiantLance2(builder);
            RadiantLance3(builder);

            return builder.Build();
        }

        private static void RadiantLance1(AbilityBuilder builder)
        {
            ConfigureRadiantLance(
                builder,
                FeatType.RadiantLance1,
                Spell.RadiantLance1,
                "Radiant Lance I",
                1,
                Rank1BaseDamage,
                4,
                15f);
        }

        private static void RadiantLance2(AbilityBuilder builder)
        {
            ConfigureRadiantLance(
                builder,
                FeatType.RadiantLance2,
                Spell.RadiantLance2,
                "Radiant Lance II",
                2,
                Rank2BaseDamage,
                5,
                15f);
        }

        private static void RadiantLance3(AbilityBuilder builder)
        {
            ConfigureRadiantLance(
                builder,
                FeatType.RadiantLance3,
                Spell.RadiantLance3,
                "Radiant Lance III",
                3,
                Rank3BaseDamage,
                6,
                18f);
        }

        private static void ConfigureRadiantLance(
            AbilityBuilder builder,
            FeatType feat,
            Spell spell,
            string name,
            int level,
            int baseDamage,
            int fp,
            float cooldown)
        {
            builder
                .Create(feat, PerkType.RadiantLance)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.None)
                .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Imp_Sunstrike)
                .UsesAuthoredAnimationAtImpact()
                .Name(name)
                .Level(level)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.RadiantLance, cooldown)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .DisplaysVisualEffectWhenActivating()
                .PlaysSoundOnImpact("ksfx_beam")
                .IsAreaAbility()
                .HasMaxRange(LineLengthMeters)
                .HasTargetingLine(
                    spell,
                    LineLengthMeters,
                    LineWidthMeters,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyRadiantLance(activator, target, targetLocation, baseDamage, level))
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(fp);
        }

        private static void ApplyRadiantLance(uint activator, uint target, Location targetLocation, int baseDamage, int level)
        {
            if (level == 3)
                LaunchRadiantLance(activator, target, targetLocation);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                baseDamage,
                0,
                null,
                CombatImpactAreaShape.Line,
                0.25f,
                LineLengthMeters,
                LineWidthMeters,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.None,
                areaVisualEffect: VisualEffect.None);
        }

        private static void LaunchRadiantLance(uint activator, uint target, Location targetLocation)
        {
            var origin = GetPosition(activator);
            var destination = GetIsObjectValid(target) ? GetPosition(target) : GetPositionFromLocation(targetLocation);
            var delta = destination - origin;
            var rotation = Math.Abs(delta.X) <= 0.01f && Math.Abs(delta.Y) <= 0.01f
                ? GetFacing(activator) * Math.PI / 180.0
                : Math.Atan2(delta.Y, delta.X);
            var end = origin + new System.Numerics.Vector3(
                (float)Math.Cos(rotation) * LineLengthMeters,
                (float)Math.Sin(rotation) * LineLengthMeters, 0f);
            var endLocation = Location(GetArea(activator), end, GetFacing(activator));

            // MIRV projectiles need an object endpoint, including casts aimed at ground.
            var projectileTarget = GetIsObjectValid(target) && target != activator
                ? target
                : CreateObject(ObjectType.Placeable, "plc_invisobj", endLocation);
            if (!GetIsObjectValid(projectileTarget))
                return;

            if (projectileTarget != target)
            {
                SetPlotFlag(projectileTarget, true);
                SetUseableFlag(projectileTarget, false);
                DestroyObject(projectileTarget, 3f);
            }

            AssignCommand(activator, () => ApplyEffectToObject(
                DurationType.Instant,
                EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv_BoltGlory),
                projectileTarget));
        }
    }
}
