using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Baseline sanity checks for the in-engine test harness itself, independent of any game
    /// system: spawning a standard blueprint into the instanced arena, and the WaitUntilAsync
    /// polling helper resolving once a condition becomes true.
    /// </summary>
    public static class HarnessSanityEngineTests
    {
        private const string SanityFlagVariable = "ENGINE_TEST_SANITY_FLAG";

        /// <summary>
        /// Spawns a benign, commoner-faction base-game creature blueprint and confirms it becomes
        /// a valid object living in the test's instanced arena.
        /// </summary>
        [EngineTest("Harness spawns a standard blueprint creature into the arena", Category = "Harness")]
        public static async Task SpawnStandardBlueprintCreature(EngineTestContext ctx)
        {
            var rat = ctx.SpawnCreature("nw_rat001");
            // Lets spawn-init settle and keeps the test on the Task-only contract.
            await ctx.WaitFrameAsync();

            ctx.Assert(GetIsObjectValid(rat), "Spawned creature should be a valid game object.");
            ctx.Assert(GetObjectType(rat) == ObjectType.Creature, "Spawned object should report as a creature.");
            ctx.AssertEqual(ctx.Arena, GetArea(rat), "Spawned creature's area");
        }

        /// <summary>
        /// Flips a local variable on a delay after the test starts, then confirms
        /// WaitUntilAsync's poll loop picks up the change and returns before its timeout.
        /// </summary>
        [EngineTest("Harness WaitUntilAsync resolves once a condition flips true", Category = "Harness", TimeoutSeconds = 20f)]
        public static async Task WaitUntilAsyncResolvesOnConditionChange(EngineTestContext ctx)
        {
            var marker = ctx.SpawnCreature("nw_rat001");
            ctx.Assert(GetLocalInt(marker, SanityFlagVariable) == 0, "Sanity flag should start unset.");

            // DelayCommand must be scheduled from a valid object's context - callbacks scheduled
            // from the async test context (no OBJECT_SELF) never fire.
            AssignCommand(marker, () => DelayCommand(1.0f, () => SetLocalInt(marker, SanityFlagVariable, 1)));

            await ctx.WaitUntilAsync(
                () => GetLocalInt(marker, SanityFlagVariable) == 1,
                10f,
                "the delayed command to set the sanity flag");

            ctx.Assert(GetLocalInt(marker, SanityFlagVariable) == 1, "Sanity flag should be set once WaitUntilAsync resolves.");
        }
    }
}
