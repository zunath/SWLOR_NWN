using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Dance I-IV and Play Instrument I-IV are PLAYER-only toggles: Performance.Toggle refuses any
    /// creature that is not a player, so the shared NPC-caster executor would only see the refusal.
    /// Every case is therefore handed to PerformanceAbilityEngineTests, which drives a native player
    /// fixture through the feat pipeline and asserts the performance mode it starts and stops.
    /// The abilities declare no FP/STM cost and no recast, so there are none to assert.
    /// </summary>
    public class PerformanceAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Performance ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 600f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new PerformanceAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // DanceAbilityDefinition - self toggle. Starts a dance session at the perk's rank, swaps the
                // hot bar for the step buttons, and marks the player Performing; the player's own Dance feat is
                // then the Stop button (routed through Performance.IsModeFeat/HandleInput, because the busy
                // check refuses it as an ordinary activation).
                new()
                {
                    Feat = FeatType.Dance1,
                    Target = AbilityTargetKind.Self,
                    RequiresPlayerPerformanceFixture = true,
                },
                new()
                {
                    Feat = FeatType.Dance2,
                    Target = AbilityTargetKind.Self,
                    RequiresPlayerPerformanceFixture = true,
                },
                new()
                {
                    Feat = FeatType.Dance3,
                    Target = AbilityTargetKind.Self,
                    RequiresPlayerPerformanceFixture = true,
                },
                new()
                {
                    Feat = FeatType.Dance4,
                    Target = AbilityTargetKind.Self,
                    RequiresPlayerPerformanceFixture = true,
                },

                // PlayInstrumentAbilityDefinition - same toggle for music. It additionally needs an equipped
                // instrument (the "guitar" blueprint), which the fixture equips.
                new()
                {
                    Feat = FeatType.PlayInstrument1,
                    Target = AbilityTargetKind.Self,
                    RequiresPlayerPerformanceFixture = true,
                    Notes = "The fixture equips the guitar blueprint; without an instrument the activation starts no session.",
                },
                new()
                {
                    Feat = FeatType.PlayInstrument2,
                    Target = AbilityTargetKind.Self,
                    RequiresPlayerPerformanceFixture = true,
                },
                new()
                {
                    Feat = FeatType.PlayInstrument3,
                    Target = AbilityTargetKind.Self,
                    RequiresPlayerPerformanceFixture = true,
                },
                new()
                {
                    Feat = FeatType.PlayInstrument4,
                    Target = AbilityTargetKind.Self,
                    RequiresPlayerPerformanceFixture = true,
                },
            };
        }
    }
}
