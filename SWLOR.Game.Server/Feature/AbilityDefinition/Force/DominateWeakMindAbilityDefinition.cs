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
    public sealed class DominateWeakMindAbilityDefinition : IAbilityListDefinition
    {
        private const int DominateWeakMindWillSaveDC = 14;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DominateWeakMind1(builder);

            return builder.Build();
        }

        private static void DominateWeakMind1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DominateWeakMind1, PerkType.DominateWeakMind)
                .Name("Dominate Weak Mind")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.DominateWeakMind, 90f)
                .SkillType(SkillType.Force)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((_, target, _, _) => ValidateNonMechanicalTarget(target))
                .HasImpactAction(DominateWeakMind1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void DominateWeakMind1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                8,
                null,
                false,
                Array.Empty<Type>(),
                statusEffectFactory: () => CreateDominateWeakMindStatusEffect(activator, target),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
        }

        private static IStatusEffect CreateDominateWeakMindStatusEffect(uint activator, uint target)
        {
            var saveResult = WillSave(target, DominateWeakMindWillSaveDC, SavingThrowType.MindSpells, activator);
            return saveResult == SavingThrowResultType.Failed
                ? new FoggyMindStatusEffect()
                : new DominateWeakMind1StatusEffect();
        }

        private static bool IsNonMechanical(uint target)
        {
            var racialType = GetRacialType(target);
            return racialType != RacialType.Construct &&
                   racialType != RacialType.Robot &&
                   racialType != RacialType.Droid;
        }

        private static string ValidateNonMechanicalTarget(uint target)
        {
            return IsNonMechanical(target)
                ? string.Empty
                : "This ability cannot affect mechanical targets.";
        }

    }
}
