using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service.EngineTestService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.EngineTestDefinition.AbilityBehaviors
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
                    ExpectedActivatorStatusEffects = new[] { typeof(Antitoxin1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // EmergencyCocktailAbilityDefinition - capstone self buff.
                new()
                {
                    Feat = FeatType.EmergencyCocktail1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(EmergencyCocktailStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Also grants temporary HP and cleanses poison/toxin; not tracked as status effect types.",
                },

                // EmergencyTriageAbilityDefinition - direct heal only, no tracked status effect;
                // ApplyTraumaMedicRiders is gated on a stat adjustment the base NPC doesn't have.
                new()
                {
                    Feat = FeatType.EmergencyTriage1,
                    Target = AbilityTargetKind.Self,
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

                // InfusionAbilityDefinition - friendly self heal-over-time status.
                new()
                {
                    Feat = FeatType.Infusion1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(RegenerativeHealingStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Infusion2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(RegenerativeHealingStatusEffect) },
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
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Zone is centered on the caster's own location (ResolveImpactLocation falls back to GetLocation(target) with target==activator); the caster is inside its own heal radius.",
                },
                new()
                {
                    Feat = FeatType.KoltoMist2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(KoltoMistHealingStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // MedKitAbilityDefinition - direct heal only, no tracked status effect.
                new()
                {
                    Feat = FeatType.MedKit1,
                    Target = AbilityTargetKind.Self,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.MedKit2,
                    Target = AbilityTargetKind.Self,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.MedKit3,
                    Target = AbilityTargetKind.Self,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.MedKit4,
                    Target = AbilityTargetKind.Self,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PainSuppressantAbilityDefinition - friendly self buff (temp HP + status).
                new()
                {
                    Feat = FeatType.PainSuppressant1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(PainSuppressant1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.PainSuppressant2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(PainSuppressant2StatusEffect) },
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
                    Target = AbilityTargetKind.Self,
                    SkipReason = "ValidateFriendlyTarget requires the target to be dead; the harness only spawns living Self/HostileCreature actors and has no way to produce a dead ally.",
                },
                new()
                {
                    Feat = FeatType.Resuscitation2,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "Same requireDead:true friendly-target requirement as Resuscitation1.",
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

                // TreatmentKitAbilityDefinition - tiers 1-2 only remove ailments (nothing to assert
                // as an applied effect); tier 3 additionally grants an unconditional status.
                new()
                {
                    Feat = FeatType.TreatmentKit1,
                    Target = AbilityTargetKind.Self,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Only removes Poison/Bleed status effects; nothing is applied to assert.",
                },
                new()
                {
                    Feat = FeatType.TreatmentKit2,
                    Target = AbilityTargetKind.Self,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Only removes TreatmentKit2-cleanseable status effects; nothing is applied to assert.",
                },
                new()
                {
                    Feat = FeatType.TreatmentKit3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(AilmentResistance3StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
