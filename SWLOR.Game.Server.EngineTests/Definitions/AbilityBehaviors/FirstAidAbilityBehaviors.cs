using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public class FirstAidAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("First Aid ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new FirstAidAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // AdrenalStimAbilityDefinition - friendly self buff. RequirementItem("stim_pack")
                // is bypassed entirely for non-PC activators (AbilityRequirementItem.CheckRequirements
                // returns success for anything that isn't a PC), so no item is needed here.
                new()
                {
                    Feat = FeatType.AdrenalStim1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(AdrenalStimStatusEffect) },
                    ExpectsRecast = true,
                    Notes = "RequirementItem is a no-op for NPC activators, so no item setup is needed. No FP/Stamina cost is declared.",
                },
                new()
                {
                    Feat = FeatType.AdrenalStim2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(AdrenalStimStatusEffect) },
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.AdrenalStim3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(AdrenalStimStatusEffect) },
                    ExpectsRecast = true,
                },

                // AntitoxinAbilityDefinition - friendly self cleanse + status.
                new()
                {
                    Feat = FeatType.Antitoxin1,
                    Target = AbilityTargetKind.Self,
                    TargetSetupStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectedRemovedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectedActivatorStatusEffects = new[] { typeof(Antitoxin1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // EmergencyCocktailAbilityDefinition - capstone self buff.
                new()
                {
                    Feat = FeatType.EmergencyCocktail1,
                    Target = AbilityTargetKind.Self,
                    TargetSetupStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectedRemovedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectedActivatorStatusEffects = new[] { typeof(EmergencyCocktailStatusEffect) },
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsRecast = true,
                    CostAssertionWaiverReason = "The impact restores more stamina than its own 15 STM cost in the same engine tick, so no post-activation pool dip can be observed.",
                    Notes = "Asserts the status, a newly added raw temporary-HP effect, and removal of a pre-applied Poison. The capstone's same-tick stamina restore masks its declared cost.",
                },

                // EmergencyTriageAbilityDefinition - direct heal only, no tracked status effect;
                // ApplyTraumaMedicRiders is gated on a stat adjustment the base NPC doesn't have.
                new()
                {
                    Feat = FeatType.EmergencyTriage1,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Heals via ApplyActivatedMedicalScaledHeal (raw EffectHeal); TraumaMedic rider requires a stat adjustment the NPC doesn't have.",
                },

                // FocusStimAbilityDefinition - friendly self buff.
                new()
                {
                    Feat = FeatType.FocusStim1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(FocusStim1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.FocusStim2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(FocusStim2StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // InfusionAbilityDefinition - friendly self heal-over-time status. The status effect's
                // own Tick (Frequency=3s) applies a guaranteed, unconditional heal each pulse, well
                // within the executor's wait window, so the caster's HP rise is also observable.
                new()
                {
                    Feat = FeatType.Infusion1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(RegenerativeHealingStatusEffect) },
                    ExpectsActivatorHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Infusion2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(RegenerativeHealingStatusEffect) },
                    ExpectsActivatorHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // KoltoMistAbilityDefinition - ground-targeted heal zone (sphere without
                // OriginOnSelf, so RequiresLocationTarget is true); Self resolves the impact
                // location to the caster's own position via ResolveImpactLocation, and the caster
                // counts as a friendly within its own zone. First heal/status pulse lands at t=3s.
                new()
                {
                    Feat = FeatType.KoltoMist1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(KoltoMistHealingStatusEffect) },
                    ExpectsActivatorHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Zone is centered on the caster's own location (ResolveImpactLocation falls back to GetLocation(target) with target==activator); the caster is inside its own heal radius and each 3s pulse also applies a raw EffectHeal via ApplyMedicalScaledHeal.",
                },
                new()
                {
                    Feat = FeatType.KoltoMist2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(KoltoMistHealingStatusEffect) },
                    ExpectsActivatorHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // MedKitAbilityDefinition - direct ally heal only, no tracked status effect.
                // Wounding a distinct friendly target proves targeting and the raw EffectHeal.
                new()
                {
                    Feat = FeatType.MedKit1,
                    Target = AbilityTargetKind.FriendlyCreature,
                    ExpectsTargetHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.MedKit2,
                    Target = AbilityTargetKind.FriendlyCreature,
                    ExpectsTargetHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.MedKit3,
                    Target = AbilityTargetKind.FriendlyCreature,
                    ExpectsTargetHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.MedKit4,
                    Target = AbilityTargetKind.FriendlyCreature,
                    ExpectsTargetHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PainSuppressantAbilityDefinition - friendly self buff (temp HP + status).
                new()
                {
                    Feat = FeatType.PainSuppressant1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(PainSuppressant1StatusEffect) },
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.PainSuppressant2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(PainSuppressant2StatusEffect) },
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ResuscitationAbilityDefinition - ValidateFriendlyTarget(requireDead: true), so it
                // requires a dead ally target. The harness only spawns living Self/HostileCreature
                // targets, and a hostile creature also fails the friendly-reaction check, so no
                // supported target kind can pass validation.
                new()
                {
                    Feat = FeatType.Resuscitation1,
                    Target = AbilityTargetKind.FriendlyCreature,
                    TargetStartsDead = true,
                    ExpectsTargetRevived = true,
                    MinimumTargetHitPointsAfterRevive = 1,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Revival cast on a dead spawned ally (requireDead:true friendly target). Native EffectResurrection returns the target with 1 HP.",
                },
                new()
                {
                    Feat = FeatType.Resuscitation2,
                    Target = AbilityTargetKind.FriendlyCreature,
                    TargetStartsDead = true,
                    ExpectsTargetRevived = true,
                    ExpectedTargetHealingPercentAfterRevive = 20f,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Derives the minimum post-revive HP from 20% of the target's maximum HP plus the caster's Willpower scaling, proving the full delayed heal rather than a token HP increase.",
                },

                // ShieldingAbilityDefinition - friendly self buff.
                new()
                {
                    Feat = FeatType.Shielding1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(Shielding1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Shielding2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(Shielding2StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Shielding3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(Shielding3StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // TreatmentKitAbilityDefinition - pre-apply every ailment promised by each rank
                // and require the real friendly-target cast to remove all of them.
                new()
                {
                    Feat = FeatType.TreatmentKit1,
                    Target = AbilityTargetKind.FriendlyCreature,
                    TargetSetupStatusEffects = new[] { typeof(BleedStatusEffect), typeof(PoisonStatusEffect) },
                    ExpectedRemovedTargetStatusEffects = new[] { typeof(BleedStatusEffect), typeof(PoisonStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.TreatmentKit2,
                    Target = AbilityTargetKind.FriendlyCreature,
                    TargetSetupStatusEffects = new[]
                    {
                        typeof(BleedStatusEffect),
                        typeof(PoisonStatusEffect),
                        typeof(ToxinStatusEffect),
                        typeof(BurnStatusEffect),
                        typeof(ShockStatusEffect),
                        typeof(DiseaseStatusEffect),
                    },
                    ExpectedRemovedTargetStatusEffects = new[]
                    {
                        typeof(BleedStatusEffect),
                        typeof(PoisonStatusEffect),
                        typeof(ToxinStatusEffect),
                        typeof(BurnStatusEffect),
                        typeof(ShockStatusEffect),
                        typeof(DiseaseStatusEffect),
                    },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.TreatmentKit3,
                    Target = AbilityTargetKind.FriendlyCreature,
                    TargetSetupStatusEffects = new[]
                    {
                        typeof(BleedStatusEffect),
                        typeof(PoisonStatusEffect),
                        typeof(ToxinStatusEffect),
                        typeof(BurnStatusEffect),
                        typeof(ShockStatusEffect),
                        typeof(DiseaseStatusEffect),
                    },
                    ExpectedRemovedTargetStatusEffects = new[]
                    {
                        typeof(BleedStatusEffect),
                        typeof(PoisonStatusEffect),
                        typeof(ToxinStatusEffect),
                        typeof(BurnStatusEffect),
                        typeof(ShockStatusEffect),
                        typeof(DiseaseStatusEffect),
                    },
                    ExpectedTargetStatusEffects = new[] { typeof(AilmentResistance3StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
