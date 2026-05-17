using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class InterruptingShotAbilityDefinition : IAbilityListDefinition
    {
        private const SkillType Skill = SkillType.Pistol;
        private const int FoggyMindActivationDelaySeconds = 2;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            InterruptingShot1(builder);
            InterruptingShot2(builder);

            return builder.Build();
        }

        private static void InterruptingShot1(AbilityBuilder builder)
        {
            InterruptingShot(builder, FeatType.InterruptingShot1, "Interrupting Shot I", level: 1, stamina: 6, InterruptingShot1ImpactAction);
        }

        private static void InterruptingShot2(AbilityBuilder builder)
        {
            InterruptingShot(builder, FeatType.InterruptingShot2, "Interrupting Shot II", level: 2, stamina: 8, InterruptingShot2ImpactAction);
        }

        private static void InterruptingShot(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int stamina,
            AbilityImpactAction impactAction)
        {
            builder
                .Create(feat, PerkType.InterruptingShot)
                .Name(name)
                .Level(level)
                .HasRecastDelay(RecastGroup.InterruptingShot, 45f)
                .HasActivationDelay(0f)
                .SkillType(Skill)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(PistolAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(impactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void InterruptingShot1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyInterruptingShot(activator, target, targetLocation, 0, 12);
        }

        private static void InterruptingShot2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyInterruptingShot(activator, target, targetLocation, 20, 20);
        }

        private static void ApplyInterruptingShot(uint activator, uint target, Location targetLocation, int baseDamage, int duration)
        {
            AssignCommand(target, () => ClearAllActions());
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                Skill,
                baseDamage,
                duration,
                typeof(FoggyMindStatusEffect),
                false,
                statusEffectFactory: FoggyMind);
        }

        private static IStatusEffect FoggyMind()
        {
            return new FoggyMindStatusEffect(FoggyMindActivationDelaySeconds);
        }
    }
}
