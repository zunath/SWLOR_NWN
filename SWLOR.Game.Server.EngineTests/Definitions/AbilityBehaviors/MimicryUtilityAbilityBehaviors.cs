using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Declarative engine-test coverage for the self/ally-facing Mimicry technique feats
    /// (SWLOR.Game.Server/Feature/AbilityDefinition/Mimicry): the three self-toggle stances
    /// (AbilityBuilder.MimicryStance + WeaponActiveAbilityDefinitionBase.ConfigureToggle) and the
    /// seven MimicryUtility() techniques (non-damaging support/self-buff actives that are exempt
    /// from the damage-element/hostility contract). None of these validate a specific equipped
    /// weapon, and all cost Stamina, never FP - same as MimicryTechniqueAbilityBehaviors.
    ///
    /// The ally-targeting utilities (FinalMandate, LastBastion, StimCanister, WardenOrder) call
    /// AbilityTargeting.GetFriendlyTargetsNearLocation, which defaults includeActivator:true and
    /// falls back to yielding the activator itself when no other party member is in range. With the
    /// harness's solo caster, that means the activator ends up as its own "ally" target, so a
    /// self-status assertion is legitimate even though the ability is nominally ally-facing (mirrors
    /// ForceAbilityBehaviors' ForceSanctuary1 case for the same reason).
    /// </summary>
    public class MimicryUtilityAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Mimicry ability behaviors (utility & stances)", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new MimicryUtilityAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // ApexCollapseTechniqueAbilityDefinition - self-toggle stance (MimicryStance +
                // ConfigureToggle); a fresh caster has no existing stance status, so ToggleSelfStatus
                // returns true and the impact unconditionally applies the stance status to self.
                new()
                {
                    Feat = FeatType.ApexCollapseTechnique,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(ApexCollapseStatusEffect) },
                    ExpectsRecast = true,
                    Notes = "Self-toggle stance; no Stamina/FP requirement is declared.",
                },

                // SustainBurnTechniqueAbilityDefinition - self-toggle stance, same ConfigureToggle pattern.
                new()
                {
                    Feat = FeatType.SustainBurnTechnique,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(SustainBurnStatusEffect) },
                    ExpectsRecast = true,
                    Notes = "Self-toggle stance; no Stamina/FP requirement is declared.",
                },

                // WardenWallTechniqueAbilityDefinition - self-toggle stance, same ConfigureToggle pattern.
                new()
                {
                    Feat = FeatType.WardenWallTechnique,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(WardenWallStatusEffect) },
                    ExpectsRecast = true,
                    Notes = "Self-toggle stance; no Stamina/FP requirement is declared.",
                },

                // FinalMandateTechniqueAbilityDefinition - MimicryUtility ally buff; applies
                // FinalMandateStatusEffect to every friendly target near the activator's location,
                // which includes the activator itself when solo.
                new()
                {
                    Feat = FeatType.FinalMandateTechnique,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(FinalMandateStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // FinishingDriveTechniqueAbilityDefinition - MimicryUtility self-buff; each cast stacks
                // (and unconditionally (re)applies) a momentum status on the activator.
                new()
                {
                    Feat = FeatType.FinishingDriveTechnique,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(FinishingDriveMomentumStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // LastBastionTechniqueAbilityDefinition - MimicryUtility capstone; grants nearby allies
                // temporary HP (raw effect, not a status effect - not asserted) and unconditionally
                // applies LastBastionStatusEffect to nearby hostiles within range (the harness's spawned
                // HostileCreature qualifies).
                new()
                {
                    Feat = FeatType.LastBastionTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(LastBastionStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Ally temporary-HP grant is a raw EffectTemporaryHitpoints, not a tracked status effect; the asserted status lands on nearby hostiles instead of the caster.",
                },

                // SnapRushTechniqueAbilityDefinition - MimicryUtility self-buff (ConfigureSelfStatus);
                // unconditionally applies Hasten1 to the activator.
                new()
                {
                    Feat = FeatType.SnapRushTechnique,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(Hasten1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    ImpactRefundsCosts = true,
                    Notes = "ConfigureSelfStatus's additionalAction refunds 6 STM (Stat.RestoreStamina) in the impact window, so only the net stamina dip is observable.",
                },

                // StimCanisterTechniqueAbilityDefinition - MimicryUtility ally buff; applies
                // StimCanisterStatusEffect to every friendly target near the activator, including itself
                // when solo.
                new()
                {
                    Feat = FeatType.StimCanisterTechnique,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(StimCanisterStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // WardenOrderTechniqueAbilityDefinition - MimicryUtility ally heal; heals nearby friendly
                // targets (including the solo activator) via a raw EffectHeal, not a tracked status effect.
                new()
                {
                    Feat = FeatType.WardenOrderTechnique,
                    Target = AbilityTargetKind.Self,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    ExpectsActivatorHealing = true,
                    Notes = "Heals via a raw EffectHeal (percent of max HP), not a tracked status effect; with the solo harness caster, GetFriendlyTargetsNearLocation yields the activator itself as the sole ally, so activator healing is observable and asserted.",
                },

                // WardenSweepTechniqueAbilityDefinition - MimicryUtility self-buff (ConfigureSelfStatus);
                // unconditionally applies WardenSweepStatusEffect to the activator.
                new()
                {
                    Feat = FeatType.WardenSweepTechnique,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(WardenSweepStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
