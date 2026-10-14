using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class BoundedWanderingEngineTests
    {
        [EngineTest("Ruin Farm wildlife roams without crossing idle bounds", Category = "BoundedWandering", TimeoutSeconds = 120f)]
        public static async Task WildlifeRoamsInsideBounds(EngineTestContext ctx)
        {
            ctx.SeedRandom(31851);
            var area = ctx.CreateInstancedArea("dan_destroyfarm");
            var entries = new DantooineSpawnDefinition().BuildSpawnTables()["DANTOOINE_RUIN_FARM"].Spawns;
            var creatures = new List<uint>();
            var origins = new List<Vector3>();
            for (var index = 0; index < entries.Count; index++)
            {
                var creature = Spawn(ctx, area, entries[index].Resref, new Vector3(122f, 25f + index * 35f, 5f));
                await ctx.DelaySecondsAsync(1f);
                creatures.Add(creature);
                origins.Add(GetPosition(creature));
                Configure(creature);
                ctx.AssertEqual(ScriptName.OnRuinFarmWildlifeHeartbeat,
                    GetEventScript(creature, EventScript.Creature_OnHeartbeat), "area-specific heartbeat is attached");
                SetLocalLocation(creature, "HOME_LOCATION", GetLocation(creature));
            }

            var maxTravel = new float[creatures.Count];
            var maxX = float.MinValue;
            var deadline = DateTime.UtcNow.AddSeconds(90);
            while (DateTime.UtcNow < deadline)
            {
                for (var index = 0; index < creatures.Count; index++)
                {
                    var position = GetPosition(creatures[index]);
                    ctx.Assert(DantooineRuinFarmWildlife.IsSafePosition(position),
                        $"{entries[index].Resref} remains inside bounds at {position}");
                    maxTravel[index] = Math.Max(maxTravel[index], Vector3.Distance(origins[index], position));
                    maxX = Math.Max(maxX, position.X);
                }
                await ctx.WaitFrameAsync();
            }
            for (var index = 0; index < creatures.Count; index++)
                ctx.Assert(maxTravel[index] > 1f, $"{entries[index].Resref} actually roamed ({maxTravel[index]:0.00}m)");
            ctx.SetResultDetail($"90s on natural 6s heartbeats, sampled every frame; maximum X={maxX:0.000}; travel=" +
                                string.Join(", ", maxTravel.Select(distance => $"{distance:0.00}m")));
        }

        [EngineTest("Bounded wildlife can pursue a combat target outside idle bounds", Category = "BoundedWandering", TimeoutSeconds = 30f)]
        public static async Task CombatMovementIsUnrestricted(EngineTestContext ctx)
        {
            var area = ctx.CreateInstancedArea("dan_destroyfarm");
            var creature = Spawn(ctx, area, "gizka", new Vector3(122f, 45f, 5f));
            var target = Spawn(ctx, area, "gizka", new Vector3(128f, 45f, 5f));
            await ctx.DelaySecondsAsync(1f);
            Configure(creature);
            SetIsTemporaryEnemy(target, creature);
            await ctx.ExecuteInCreatureContextAsync(creature, () => Enmity.ModifyEnmity(target, creature, 10000));
            await ctx.ExecuteInCreatureContextAsync(creature, DantooineRuinFarmWildlife.Heartbeat);
            await ctx.WaitUntilAsync(() => GetPosition(creature).X > 124.25f, 12f,
                "wildlife to pursue its target past the idle-only boundary");
            await ctx.ExecuteInCreatureContextAsync(creature, DantooineRuinFarmWildlife.Heartbeat);
            ctx.AssertEqual(target, Enmity.GetHighestEnmityTarget(creature), "bounds do not interrupt active combat pursuit");
            ctx.SetResultDetail($"Combat pursuit crossed to X={GetPosition(creature).X:0.000}");
        }

        [EngineTest("Idle wildlife returns from outside bounds even with a queued movement", Category = "BoundedWandering", TimeoutSeconds = 30f)]
        public static async Task IdleRecoveryInterruptsOutsideMovement(EngineTestContext ctx)
        {
            var area = ctx.CreateInstancedArea("dan_destroyfarm");
            var creature = Spawn(ctx, area, "gizka", new Vector3(127f, 35f, 5f));
            await ctx.DelaySecondsAsync(1f);
            Configure(creature);
            var home = Location(area, new Vector3(122f, 35f, 5f), 0f);
            SetLocalLocation(creature, "HOME_LOCATION", home);
            await ctx.ExecuteInCreatureContextAsync(creature,
                () => ActionMoveToLocation(Location(area, new Vector3(129f, 35f, 5f), 0f)));
            await ctx.ExecuteInCreatureContextAsync(creature, DantooineRuinFarmWildlife.Heartbeat);
            ctx.AssertEqual(122f, GetPositionFromLocation(GetLocalLocation(creature, "HOME_LOCATION")).X,
                "spawn initialization has finished and retains the recovery home");
            ctx.Assert(!GetIsInCombat(creature) && !GetIsObjectValid(Enmity.GetHighestEnmityTarget(creature)),
                "recovery is tested on idle wildlife");
            await ctx.WaitUntilAsync(() => GetPosition(creature).X < 124f, 12f,
                $"idle recovery from {GetPosition(creature)}; action={GetCurrentAction(creature)}");
            ctx.SetResultDetail($"Outside idle movement recovered to X={GetPosition(creature).X:0.000}");
        }

        [EngineTest("Wander bounds cannot restrict player movement", Category = "BoundedWandering", TimeoutSeconds = 30f)]
        public static async Task PlayersRemainUnrestricted(EngineTestContext ctx)
        {
            var area = ctx.CreateInstancedArea("dan_destroyfarm");
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 0f, (fixture, stage) =>
            {
                if (stage != PlayerAbilityFixture.IdentitySetupStage.RecordPersisted)
                    return;
                // A headless client cannot acknowledge an area load. Enter before registering
                // its PC identity, then exercise ordinary walking with that identity installed.
                var server = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp;
                var native = server.GetGameObject(fixture.Creature).AsNWSCreature();
                native.RemoveFromArea();
                native.AddToArea(server.GetGameObject(area).AsNWSArea(), 117f, 75f, 5f, 0, 0);
            });
            ctx.AssertEqual(area, GetArea(player.Creature), "player is in the Ruined Farmlands instance");
            var originalHeartbeat = GetEventScript(player.Creature, EventScript.Creature_OnHeartbeat);
            DantooineRuinFarmWildlife.AttachHeartbeat(player.Creature);
            ctx.AssertEqual(originalHeartbeat, GetEventScript(player.Creature, EventScript.Creature_OnHeartbeat),
                "players cannot receive the wildlife heartbeat");
            await ctx.ExecuteInCreatureContextAsync(player.Creature, DantooineRuinFarmWildlife.Heartbeat);
            await ctx.ExecuteInCreatureContextAsync(player.Creature,
                () => ActionMoveToLocation(Location(area, new Vector3(129.5f, 75f, 5f), 0f), true));
            await ctx.WaitUntilAsync(() => GetPosition(player.Creature).X > 129f, 12f,
                "the player to walk through the NPC-only boundary");
            ctx.SetResultDetail($"Player walked to X={GetPosition(player.Creature).X:0.000}");
        }

        private static uint Spawn(EngineTestContext ctx, uint area, string resref, Vector3 position)
        {
            var creature = CreateObject(ObjectType.Creature, resref, Location(area, position, 0f));
            ctx.Assert(GetIsObjectValid(creature), $"Spawned {resref}");
            ctx.Track(creature);
            ChangeToStandardFaction(creature, StandardFaction.Defender);
            SetPlotFlag(creature, true);
            SetAILevel(creature, AILevel.VeryHigh);
            return creature;
        }

        private static void Configure(uint creature)
        {
            var entry = new DantooineSpawnDefinition().BuildSpawnTables()["DANTOOINE_RUIN_FARM"].Spawns
                .Single(spawn => spawn.Resref == GetResRef(creature));
            AI.SetAIFlag(creature, entry.AIFlags);
            foreach (var action in entry.OnSpawnActions)
                action(creature);
            SetLocalLocation(creature, "HOME_LOCATION", GetLocation(creature));
        }

        [EngineTest("Ruin Farm idle heartbeat cost is measured locally", Category = "BoundedWandering", TimeoutSeconds = 30f)]
        public static async Task IdleHeartbeatCost(EngineTestContext ctx)
        {
            var area = ctx.CreateInstancedArea("dan_destroyfarm");
            var creature = Spawn(ctx, area, "gizka", new Vector3(122f, 75f, 5f));
            await ctx.DelaySecondsAsync(1f);
            Configure(creature);
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                const int samples = 200;
                double baselineTotal = 0, roamingTotal = 0, roamingMax = 0;
                for (var index = 0; index < samples; index++)
                {
                    ClearAllActions();
                    var start = Stopwatch.GetTimestamp();
                    AI.CreatureHeartbeat();
                    baselineTotal += Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    ClearAllActions();
                    start = Stopwatch.GetTimestamp();
                    DantooineRuinFarmWildlife.Heartbeat();
                    var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    roamingTotal += elapsed;
                    roamingMax = Math.Max(roamingMax, elapsed);
                }
                ClearAllActions();
                ctx.SetResultDetail($"{samples} paired local samples: shared mean={baselineTotal / samples:0.000}ms, " +
                    $"area-specific mean={roamingTotal / samples:0.000}ms, max={roamingMax:0.000}ms. " +
                    "A local microbenchmark, not a production load test.");
            });
        }
    }
}
