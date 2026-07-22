using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service.EngineTestService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.EngineTestDefinition.AbilityBehaviors
{
    /// <summary>
    /// Declarative engine-test coverage for every FeatType registered by the Lightsaber
    /// ability definitions (SWLOR.Game.Server/Feature/AbilityDefinition/Lightsaber).
    /// </summary>
    public class LightsaberAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string LightsaberResref = "vet_trnsaber";

        [EngineTest("Lightsaber ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new LightsaberAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // AegisEternalAbilityDefinition - capstone self-buff, replaces SaberWard on activation.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.AegisEternal1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = LightsaberResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(PerfectAegisStatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Capstone self-toggle; also replaces an active SaberWardStatusEffect on activation (not asserted)."
                },

                // EpicenterAbilityDefinition - capstone hostile AoE, Force damage + unconditional Knockdown/Sunder.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.Epicenter1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect), typeof(SunderStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Capstone hostile area impact (Force damage). Extra damage vs Sunder-afflicted targets not asserted (conditional)."
                },

                // ForceLinkAbilityDefinition - requires a non-self friendly ally target (ValidateFriendlyTarget disallows self).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ForceLink1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = LightsaberResref,
                    SkipReason = "Requires a friendly (non-self) target - AbilityTargeting.ValidateFriendlyTarget is called with allowSelf:false, and the engine test harness only supports Self or HostileCreature targeting, so no ally can be supplied."
                },

                // ForceSheathAbilityDefinition - queued weapon ability, Force damage on next landed hit.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ForceSheath1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Weapon-queued ability; Force damage lands on the next auto-attack (not asserted here)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ForceSheath2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Weapon-queued ability; Force damage lands on the next auto-attack (not asserted here)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ForceSheath3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Weapon-queued ability; Force damage lands on the next auto-attack (not asserted here)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ForceSheath4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Weapon-queued ability; Force damage lands on the next auto-attack (not asserted here)."
                },

                // GuardiansChallengeAbilityDefinition - hostile area line taunt/damage, self-enmity is conditional.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.GuardiansChallenge1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus self-enmity only applies if the target recently damaged the activator (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.GuardiansChallenge2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus self-enmity only applies if the target recently damaged the activator (conditional, not asserted)."
                },

                // ImbuementStanceAbilityDefinition - self-toggle stance, no resource cost.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ImbuementStance1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = LightsaberResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(ImbuementStanceStatusEffect) },
                    ExpectsRecast = true
                },

                // ImmovableStanceAbilityDefinition - self-toggle stance, no resource cost.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ImmovableStance1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = LightsaberResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(ImmovableStanceStatusEffect) },
                    ExpectsRecast = true
                },

                // ReprisalAbilityDefinition - hostile single-target damage, Dazed status is conditional.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.Reprisal1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Dazed status only applies if the target recently damaged the activator (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.Reprisal2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Dazed status only applies if the target recently damaged the activator (conditional, not asserted)."
                },

                // SaberWardAbilityDefinition - self-toggle stance, replaces PerfectAegis on activation.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SaberWard1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = LightsaberResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(SaberWardStatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SaberWard2,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = LightsaberResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(SaberWardStatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SaberWard3,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = LightsaberResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(SaberWardStatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SaberWard4,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = LightsaberResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(SaberWardStatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // ShatteringStrikeAbilityDefinition - hostile single-target damage + unconditional Sunder.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ShatteringStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectedTargetStatusEffects = new[] { typeof(SunderStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ShatteringStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectedTargetStatusEffects = new[] { typeof(SunderStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // SunderingSweepAbilityDefinition - hostile area damage; Sunder spread requires the target
                // to already be Sundered, so it is conditional and not asserted.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SunderingSweep1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Sunder spread-from-target only fires if the target is already Sundered (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SunderingSweep2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Sunder spread-from-target only fires if the target is already Sundered (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SunderingSweep3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = LightsaberResref,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Sunder spread-from-target only fires if the target is already Sundered (conditional, not asserted)."
                }
            };
        }
    }
}
