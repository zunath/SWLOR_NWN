using System.Threading.Tasks;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// A searched scavenge point that still holds items despawns a few minutes after it is closed,
    /// freeing its spawn slot, instead of waiting out its spawn table's natural despawn.
    /// </summary>
    public static class ScavengeLeftoverDespawnEngineTests
    {
        private const string NodeResref = "fiberp_bush_3";
        private const string SpawnTableId = "RESOURCES_HUTLAR_QIONTUNDRA";
        private const string LeftoverItemResref = "fiberp_good";

        [EngineTest("A searched scavenge point left with items despawns after the leftover delay and frees its spawn slot", Category = "ScavengeLeftoverDespawn", TimeoutSeconds = 30f)]
        public static async Task LeftoverNodeDespawnsAndFreesSlot(EngineTestContext ctx)
        {
            var node = CreateNode(ctx);
            var spawnId = Spawn.RegisterResourceSpawnForTesting(node, SpawnTableId);
            try
            {
                var naturalDespawn = Spawn.GetResourceDespawnTimeForTesting(node);
                ctx.Assert(naturalDespawn.HasValue && naturalDespawn.Value > DateTime.UtcNow.AddMinutes(60),
                    $"The node should start with its table's natural despawn (90-150 minutes), but it was {naturalDespawn}.");

                await AddLeftoverItem(ctx, node);

                // Closing an unsearched node leaves its natural despawn alone.
                await Close(ctx, node);
                ctx.AssertEqual(naturalDespawn, Spawn.GetResourceDespawnTimeForTesting(node), "Despawn time after closing an unsearched node");

                SetLocalBool(node, "FULLY_HARVESTED", true);
                var closedAt = DateTime.UtcNow;
                await Close(ctx, node);
                var leftoverDespawn = Spawn.GetResourceDespawnTimeForTesting(node);
                var expected = closedAt.AddMinutes(Spawn.LeftoverResourceDespawnMinutes);
                ctx.Assert(leftoverDespawn.HasValue && Math.Abs((leftoverDespawn.Value - expected).TotalSeconds) < 5,
                    $"A searched node with items should despawn {Spawn.LeftoverResourceDespawnMinutes} minutes after closing (~{expected:O}), but it was {leftoverDespawn:O}.");

                // Closing it again later does not push the despawn back.
                await ctx.DelaySecondsAsync(1f);
                await Close(ctx, node);
                ctx.AssertEqual(leftoverDespawn, Spawn.GetResourceDespawnTimeForTesting(node), "Despawn time after closing the node again");

                await ctx.ExecuteInCreatureContextAsync(GetModule(), () => Spawn.ProcessResourceDespawnsForTesting(closedAt.AddMinutes(Spawn.LeftoverResourceDespawnMinutes - 1)));
                await NwTask.NextFrame();
                ctx.Assert(GetIsObjectValid(node), "The node should remain before the leftover delay passes.");
                ctx.Assert(Spawn.IsSpawnActiveForTesting(spawnId), "The node should still hold its spawn slot before the leftover delay passes.");
                ctx.AssertEqual(0, Spawn.GetQueuedSpawnCountForTesting(spawnId), "Queued respawns before the leftover delay passes");

                await ctx.ExecuteInCreatureContextAsync(GetModule(), () => Spawn.ProcessResourceDespawnsForTesting(closedAt.AddMinutes(Spawn.LeftoverResourceDespawnMinutes + 1)));
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(node), 5f, "the leftover node to be destroyed");
                ctx.Assert(!Spawn.IsSpawnActiveForTesting(spawnId), "The despawned node should free its spawn slot.");
                ctx.AssertEqual(1, Spawn.GetQueuedSpawnCountForTesting(spawnId), "Queued respawns after the leftover node despawns");

                await ctx.ExecuteInCreatureContextAsync(GetModule(), () => Spawn.ProcessResourceDespawnsForTesting(closedAt.AddMinutes(Spawn.LeftoverResourceDespawnMinutes + 2)));
                ctx.AssertEqual(1, Spawn.GetQueuedSpawnCountForTesting(spawnId), "Queued respawns after another despawn pass");
            }
            finally
            {
                Spawn.RemoveSpawnForTesting(spawnId);
            }
        }

        [EngineTest("A searched scavenge point emptied before its leftover despawn queues one respawn", Category = "ScavengeLeftoverDespawn", TimeoutSeconds = 30f)]
        public static async Task EmptiedBeforeTimerQueuesOneRespawn(EngineTestContext ctx)
        {
            var node = CreateNode(ctx);
            var spawnId = Spawn.RegisterResourceSpawnForTesting(node, SpawnTableId);
            try
            {
                var item = await AddLeftoverItem(ctx, node);
                SetLocalBool(node, "FULLY_HARVESTED", true);
                var closedAt = DateTime.UtcNow;
                await Close(ctx, node);
                ctx.Assert(Spawn.GetResourceDespawnTimeForTesting(node).HasValue, "Closing the searched node should schedule its leftover despawn.");

                // The player comes back and takes the rest, then closes it.
                await ctx.ExecuteInCreatureContextAsync(GetModule(), () => DestroyObject(item));
                await NwTask.NextFrame();
                await Close(ctx, node);
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(node), 5f, "the emptied node to die");
                ctx.Assert(!Spawn.IsSpawnActiveForTesting(spawnId), "The emptied node should free its spawn slot.");
                ctx.AssertEqual(1, Spawn.GetQueuedSpawnCountForTesting(spawnId), "Queued respawns after the node is emptied");

                await ctx.ExecuteInCreatureContextAsync(GetModule(), () => Spawn.ProcessResourceDespawnsForTesting(closedAt.AddMinutes(Spawn.LeftoverResourceDespawnMinutes + 1)));
                await NwTask.NextFrame();
                ctx.AssertEqual(1, Spawn.GetQueuedSpawnCountForTesting(spawnId), "Queued respawns after the leftover timer would have fired");
                ctx.Assert(!Spawn.GetResourceDespawnTimeForTesting(node).HasValue, "The emptied node's despawn entry should be cleared.");
            }
            finally
            {
                Spawn.RemoveSpawnForTesting(spawnId);
            }
        }

        [EngineTest("An emptied desert shrub dies, frees its spawn slot, and queues one respawn", Category = "ScavengeLeftoverDespawn", TimeoutSeconds = 30f)]
        public static async Task EmptiedDesertShrubQueuesRespawn(EngineTestContext ctx)
        {
            var node = CreateNode(ctx, "desert_shrub");
            var spawnId = Spawn.RegisterResourceSpawnForTesting(node, "KORRIBAN_WASTELANDS_RESOURCES");
            try
            {
                SetLocalBool(node, "FULLY_HARVESTED", true);
                await Close(ctx, node);
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(node), 5f, "the emptied shrub to die");
                ctx.Assert(!Spawn.IsSpawnActiveForTesting(spawnId), "The emptied shrub should free its spawn slot.");
                ctx.AssertEqual(1, Spawn.GetQueuedSpawnCountForTesting(spawnId), "Queued respawns after the shrub is emptied");
            }
            finally
            {
                Spawn.RemoveSpawnForTesting(spawnId);
            }
        }

        [EngineTest("A hand-placed scavenge point left with items is not despawned", Category = "ScavengeLeftoverDespawn", TimeoutSeconds = 30f)]
        public static async Task HandPlacedNodeIsLeftAlone(EngineTestContext ctx)
        {
            var node = CreateNode(ctx);
            await AddLeftoverItem(ctx, node);
            SetLocalBool(node, "FULLY_HARVESTED", true);

            await Close(ctx, node);
            ctx.Assert(!Spawn.GetResourceDespawnTimeForTesting(node).HasValue, "A node the spawn system did not create should not be scheduled to despawn.");
            ctx.Assert(GetIsObjectValid(node), "A hand-placed node with items should remain.");
        }

        // Item creation and death effects need a script context, which async continuations lack.
        private static async Task<uint> AddLeftoverItem(EngineTestContext ctx, uint node)
        {
            var item = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(GetModule(), () => item = CreateItemOnObject(LeftoverItemResref, node));
            ctx.Assert(GetIsObjectValid(item), $"Item '{LeftoverItemResref}' should be created in the node.");
            return item;
        }

        private static Task Close(EngineTestContext ctx, uint node)
        {
            return ctx.ExecuteInCreatureContextAsync(GetModule(), () => ScavengePoint.ResolveClosedScavengePoint(node));
        }

        private static uint CreateNode(EngineTestContext ctx, string resref = NodeResref)
        {
            var node = CreateObject(ObjectType.Placeable, resref, ctx.GetArenaLocation(2f));
            ctx.Assert(GetIsObjectValid(node), $"Placeable '{resref}' should be created.");
            ctx.Track(node);
            return node;
        }
    }
}
