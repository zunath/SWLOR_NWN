using System.Collections.Generic;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerFixtureLifecycleEngineTests
    {
        /// <summary>
        /// Interrupts each identity setup stage and verifies that no persisted record,
        /// search document or native roster entry escapes rollback. A surviving fixture
        /// proves cleanup does not unregister other players.
        /// </summary>
        [EngineTest("Player fixture rolls back partial identity setup", Category = "PlayerFixture", TimeoutSeconds = 45f)]
        public static async Task PartialSetupCleanup(EngineTestContext ctx)
        {
            using var survivor = await PlayerAbilityFixture.CreateAsync(ctx);
            var expectedRoster = GetPlayerRoster();
            var expectedRecords = DB.SearchCount(new DBQuery<Player>());
            foreach (var failureStage in Enum.GetValues<PlayerAbilityFixture.IdentitySetupStage>())
            {
                PlayerAbilityFixture interrupted = null;
                var originalPlayerFlag = 0;
                var expectedFailure = new InvalidOperationException($"Interrupt identity setup at {failureStage}.");
                var sawExpectedFailure = false;
                try
                {
                    using var unexpected = await PlayerAbilityFixture.CreateAsync(ctx, 2f, (fixture, stage) =>
                    {
                        interrupted = fixture;
                        if (stage == PlayerAbilityFixture.IdentitySetupStage.RecordPersisted)
                            originalPlayerFlag = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(fixture.Creature).AsNWSCreature().m_bPlayerCharacter;
                        if (stage == failureStage)
                            throw expectedFailure;
                    });
                }
                catch (InvalidOperationException failure) when (ReferenceEquals(failure, expectedFailure))
                {
                    sawExpectedFailure = true;
                }
                ctx.Assert(sawExpectedFailure, $"{failureStage}: setup reports the original failure");
                ctx.Assert(!DB.Exists<Player>(interrupted.Id), $"{failureStage}: player JSON is removed");
                ctx.AssertEqual(expectedRecords, DB.SearchCount(new DBQuery<Player>()), $"{failureStage}: search document is removed");
                ctx.Assert(GetPlayerRoster().SetEquals(expectedRoster), $"{failureStage}: native roster is restored");
                ctx.AssertEqual(originalPlayerFlag,
                    NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(interrupted.Creature).AsNWSCreature().m_bPlayerCharacter,
                    $"{failureStage}: original native player flag is restored");
                interrupted.Dispose();
                ctx.Assert(GetPlayerRoster().SetEquals(expectedRoster), $"{failureStage}: repeated cleanup is harmless");
            }
            ctx.Assert(DB.Exists<Player>(survivor.Id), "rollback preserves the other player's record");
        }

        /// <summary>
        /// Returns a snapshot so later native player enumeration cannot alter comparisons.
        /// </summary>
        private static HashSet<uint> GetPlayerRoster()
        {
            var players = new HashSet<uint>();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                players.Add(player);
            return players;
        }
    }
}
