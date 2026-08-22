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
    public sealed class ForceChokeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceChoke1(builder);
            ForceChoke2(builder);
            ForceChoke3(builder);
            ForceChoke4(builder);

            return builder.Build();
        }

        private static void ForceChoke1(AbilityBuilder builder)
        {
            ConfigureForceChoke(builder, FeatType.ForceChoke1, "Force Choke I", 1, 2, 30, 8);
        }

        private static void ForceChoke2(AbilityBuilder builder)
        {
            ConfigureForceChoke(builder, FeatType.ForceChoke2, "Force Choke II", 2, 3, 30, 16);
        }

        private static void ForceChoke3(AbilityBuilder builder)
        {
            ConfigureForceChoke(builder, FeatType.ForceChoke3, "Force Choke III", 3, 4, 30, 24);
        }

        private static void ForceChoke4(AbilityBuilder builder)
        {
            ConfigureForceChoke(builder, FeatType.ForceChoke4, "Force Choke IV", 4, 5, 30, 34);
        }

        private static void ConfigureForceChoke(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int fp,
            int immobilizeSeconds,
            int totalDamage)
        {
            builder
                .Create(feat, PerkType.ForceChoke)
                .Name(name)
                .Level(level)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ForceChoke, 45f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_frc_choke")
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyForceChoke(activator, target, targetLocation, immobilizeSeconds, totalDamage))
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(fp);
        }

        private static void ApplyForceChoke(
            uint activator,
            uint target,
            Location targetLocation,
            int immobilizeSeconds,
            int totalDamage)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                immobilizeSeconds,
                typeof(ImmobilizedStatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                afterSuccessfulHit: hitTarget => ApplyForceChokeEffects(activator, hitTarget, immobilizeSeconds, totalDamage));
        }

        private static void ApplyForceChokeEffects(uint activator, uint target, int immobilizeSeconds, int totalDamage)
        {
            AssignCommand(target, () => ClearAllActions());
            AssignCommand(target, () => ActionPlayAnimation(Animation.ForceChoke));
            ApplyForceDamageOverTime(activator, target, immobilizeSeconds, totalDamage);
        }

        private static void ApplyForceDamageOverTime(uint activator, uint target, int immobilizeSeconds, int totalDamage)
        {
            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new ForceChokeDamageStatusEffect(totalDamage),
                immobilizeSeconds,
                CombatDamageType.Force);
        }

    }
}
