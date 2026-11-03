using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public abstract class TwinPistolAbilityDefinitionBase : WeaponActiveAbilityDefinitionBase
    {
        protected static void ConfigurePairAttack(
            AbilityBuilder builder, FeatType feat, PerkType perk, string name, int level,
            RecastGroup recastGroup, float cooldown, TwinPistolAbilitySpec spec, float activationDelay = 0f,
            AnimationClip channelAnimation = null)
        {
            builder.Create(feat, perk)
                .Name(name)
                .Level(level)
                .SkillType(SkillType.TwinPistol)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasActivationDelay(activationDelay)
                .HasRecastDelay(recastGroup, cooldown)
                .HasMaxRange(spec.MaxRange)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Com_Blood_Spark_Small)
                .HasCustomValidation((actor, target, _, _) => TwinPistolAbility.Validate(actor, target, spec))
                .HasActivationAction((actor, target, _, _) => TwinPistolAbility.Begin(actor, target, spec))
                .HasImpactAction((actor, _, _, _) => TwinPistolAbility.Complete(actor, spec))
                .RequirementStamina(spec.StaminaCost)
                .IsSingleTargetAbility()
                .RequiresTarget();

            if (spec.CadencePowerPerCharge > 0)
                builder.HasWeaponAbilityCadenceDamagePerCharge(spec.CadencePowerPerCharge);
            if (!spec.SpendsCadence)
                builder.KeepsWeaponAbilityCadence();
            if (spec.CadenceOnHit > 0)
                builder.GrantsWeaponAbilityCadence(spec.CadenceOnHit);

            if (spec.VolleyCount > 1)
                builder.IsChanneledAbility(TwinPistolAbility.InterruptChannel, suppressAutoAttacks: true)
                    .HasFixedActivationDelay(spec.ChannelSeconds)
                    .UsesAnimation(channelAnimation);
            else
                builder.UsesAuthoredAnimationAtImpact();
        }

        protected static void ConfigurePairStance(
            AbilityBuilder builder, FeatType feat, PerkType perk, string name, RecastGroup recastGroup, Type status)
        {
            builder.Create(feat, perk)
                .Name(name)
                .Level(1)
                .SkillType(SkillType.TwinPistol)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .HasRecastDelay(recastGroup, 30f)
                .RequirementStamina(0)
                .UsesImmediateAuthoredAnimation()
                .UsesAnimation(Animation.CastOutAnimation)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Imp_Haste);
            ConfigureToggle(builder, status);
        }
    }
}
