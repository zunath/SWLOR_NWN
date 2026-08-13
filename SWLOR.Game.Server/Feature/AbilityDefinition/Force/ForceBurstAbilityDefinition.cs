using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ForceBurstAbilityDefinition : IAbilityListDefinition
    {
        private const float RadiusMeters = 5f;
        private const int Rank1BaseDamage = 18;
        private const int Rank2BaseDamage = 34;
        private const int Rank3BaseDamage = 50;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureForceBurst(
                builder,
                FeatType.ForceBurst1,
                Spell.ForceBurst1,
                "Force Burst I",
                1,
                Rank1BaseDamage,
                4);
            ConfigureForceBurst(
                builder,
                FeatType.ForceBurst2,
                Spell.ForceBurst2,
                "Force Burst II",
                2,
                Rank2BaseDamage,
                5);
            ConfigureForceBurst(
                builder,
                FeatType.ForceBurst3,
                Spell.ForceBurst3,
                "Force Burst III",
                3,
                Rank3BaseDamage,
                6);

            return builder.Build();
        }

        private static void ConfigureForceBurst(
            AbilityBuilder builder,
            FeatType feat,
            Spell spell,
            string name,
            int level,
            int baseDamage,
            int fp)
        {
            builder
                .Create(feat, PerkType.ForceBurst)
                .Name(name)
                .Level(level)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ForceBurst, 15f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .DisplaysVisualEffectWhenActivating()
                .PlaysSoundOnImpact("plr_force_blast")
                .IsAreaAbility()
                .HasTargetingSphere(
                    spell,
                    RadiusMeters,
                    AbilityTargetingFlags.HarmsEnemies)
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyForceBurst(activator, target, targetLocation, baseDamage))
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(fp);
        }

        private static void ApplyForceBurst(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                baseDamage,
                0,
                null,
                CombatImpactAreaShape.Sphere,
                0f,
                RadiusMeters,
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Wind,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Screen_Bump);
        }
    }
}
