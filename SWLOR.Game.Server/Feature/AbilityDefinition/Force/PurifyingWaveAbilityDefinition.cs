using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class PurifyingWaveAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PurifyingWave1(builder);

            return builder.Build();
        }

        private static void PurifyingWave1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PurifyingWave1, PerkType.PurifyingWave)
                .Name("Purifying Wave")
                .Level(1)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_frc_wave")
                .HasRecastDelay(RecastGroup.PurifyingWave, 45f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .IsAreaAbility()
                .HasImpactAction(PurifyingWave1ImpactAction)
                .HasTargetingSphere(
                    Spell.PurifyingWave1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(7);
        }

        private static void PurifyingWave1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var impactLocation = GetLocation(activator);

            foreach (var hostile in AbilityTargeting.GetHostileTargetsNearLocation(activator, impactLocation, 5f, 0))
            {
                Ability.ApplyCombatImpact(
                    activator,
                    hostile,
                    GetLocation(hostile),
                    SkillType.Force,
                    22,
                    0,
                    null,
                    false,
                    damageType: CombatDamageType.Force,
                    targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Holy,
                    playImpactAnimation: false);
            }

            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, true))
            {
                StatusEffect.RemoveFirstCleanseableStatusEffect(friendly, StatusEffectCleanseType.Purify, false);
                AbilityEffectScaling.ApplyActivatedScaledHeal(activator, friendly, 8);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
            }
        }
    }
}
