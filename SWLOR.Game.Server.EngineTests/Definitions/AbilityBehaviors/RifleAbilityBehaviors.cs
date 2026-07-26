using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Declarative engine-test coverage for every FeatType registered by the Rifle
    /// ability definitions (SWLOR.Game.Server/Feature/AbilityDefinition/Rifle).
    /// </summary>
    public class RifleAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string RifleResref = "b_rifle";

        [EngineTest("Rifle ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new RifleAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // AimedShotAbilityDefinition - hostile single-target damage; idle bonus is conditional.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.AimedShot1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus damage while idle is conditional (requires no recent attack activity); not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.AimedShot2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus damage while idle is conditional (requires no recent attack activity); not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.AimedShot3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus damage while idle is conditional (requires no recent attack activity); not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.AimedShot4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus damage while idle is conditional (requires no recent attack activity); not asserted."
                },

                // CripplingShotAbilityDefinition - hostile damage + unconditional Hamstring.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.CripplingShot1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.CripplingShot2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.CripplingShot3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // HeadshotAbilityDefinition - hostile damage; the crit-rate bonuses are combat-roll
                // modifiers (one of which is dead code - see Notes), not status effect types.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.Headshot1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Crit-rate adjustment is a hit-roll modifier, not a status effect; SelfCriticalRatePercent never applies because SelfStatDurationSeconds is unset (0) in this profile."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.Headshot2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Crit-rate adjustment is a hit-roll modifier, not a status effect; SelfCriticalRatePercent never applies because SelfStatDurationSeconds is unset (0) in this profile."
                },

                // KillBoxAbilityDefinition - capstone hostile AoE; riders are conditional temporary
                // stat modifiers gated behind a defeated enemy, not status effect types.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.KillBox1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Suppression/defeated-enemy riders are conditional temporary stat modifiers, not status effect classes; not asserted."
                },

                // OneShotAbilityDefinition - capstone hostile single-target; crit/defense-ignore are
                // hit-roll modifiers, not status effect types.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.OneShot1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Crit-rate/defense-ignore adjustments are hit-roll modifiers, not status effects; SelfCriticalRatePercent never applies (SelfStatDurationSeconds unset)."
                },

                // PiercingRoundAbilityDefinition - hostile area line damage; defense-ignore is a
                // hit-roll modifier, not a status effect type.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PiercingRound1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Defense-ignore is a hit-roll modifier, not a status effect; not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PiercingRound2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Defense-ignore is a hit-roll modifier, not a status effect; not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PiercingRound3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Defense-ignore is a hit-roll modifier, not a status effect; not asserted."
                },

                // SniperStanceAbilityDefinition - self-toggle stance, no resource cost.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SniperStance1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = RifleResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(SniperStanceStatusEffect) },
                    ExpectsRecast = true
                },

                // SuppressingShotAbilityDefinition - hostile damage + an unconditional Suppression
                // stack on hit (Combat.ApplySuppressionStack applies SuppressionStatusEffect whenever
                // the computed evasion penalty is above zero, which it always is for these profiles).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SuppressingShot1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectedTargetStatusEffects = new[] { typeof(SuppressionStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SuppressingShot2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectedTargetStatusEffects = new[] { typeof(SuppressionStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SuppressingShot3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectedTargetStatusEffects = new[] { typeof(SuppressionStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SuppressingShot4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectedTargetStatusEffects = new[] { typeof(SuppressionStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // SuppressionStanceAbilityDefinition - self-toggle stance, no resource cost.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SuppressionStance1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = RifleResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(SuppressionStanceStatusEffect) },
                    ExpectsRecast = true
                },

                // SuppressiveLineAbilityDefinition - hostile area line damage; Disorient requires 2
                // pre-existing suppression stacks (conditional, not present on a fresh target) and this
                // profile does not itself apply a suppression stack (ApplySuppressionStackOnHit unset).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SuppressiveLine1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Disorient requires 2 pre-existing suppression stacks on the target (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SuppressiveLine2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = RifleResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Disorient requires 2 pre-existing suppression stacks on the target (conditional, not asserted)."
                }
            };
        }
    }
}
