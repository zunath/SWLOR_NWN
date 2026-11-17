using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// An area with live spawns across hot reloads (the P5-8c test plan: the spawn registry adopts and no area spawns twice).
    /// Like <see cref="HotReloadPlayersEngineTests"/> it runs in phases, one per generation of the build:
    /// <list type="number">
    /// <item>The first run puts a headless player in the Abandoned Station, enters the area through the spawn handler, waits for
    /// its spawns to be placed, sends three back to the respawn queue and records the registry's active and queued spawns for the area
    /// and the spawned objects standing in it.</item>
    /// <item>Each run after a reload reads the same counts from the new build's registry and the world, and requires the
    /// recorded objects to be alive and still registered, every spawned object to belong to one active spawn (no spawn placed
    /// twice) and the area's active plus queued spawns to equal the recorded total.</item>
    /// <item>The run after the second reload also takes the player and the area's spawns away.</item>
    /// </list>
    /// It reads the registry through <see cref="SpawnInternals"/> and reflection only, so it runs unchanged on builds before and
    /// after the spawn registry split. Run it with <c>scripts/run-hot-reload-check.sh --engine-filter HotReloadSpawns --reloads 2</c>.
    /// </summary>
    public static class HotReloadSpawnsEngineTests
    {
        private const string Prefix = "SWLOR_HRS_";
        private const string AreaResref = "zomb_abanstation";
        private const string LandingTag = "ABANDONED_STATION_DOCK_LANDING";
        private const int ReloadsToVerify = 2;
        private static readonly string GenerationToken = Guid.NewGuid().ToString("N");

        [EngineTest("An area with live and queued spawns keeps them across hot reloads and spawns nothing twice", Category = "HotReloadSpawns",
            TimeoutSeconds = 120f)]
        public static async Task SpawnsStayAcrossReloads(EngineTestContext ctx)
        {
            if (Environment.GetEnvironmentVariable("SWLOR_HOT_RELOAD")?.Trim().ToLowerInvariant() is not ("true" or "1" or "yes"))
                ctx.Skip("Hot reload is off; run scripts/run-hot-reload-check.sh --engine-filter HotReloadSpawns --reloads 2.");

            var module = GetModule();
            if (GetLocalInt(module, Prefix + "PREPARED") == 0)
            {
                await PrepareAsync(ctx);
                return;
            }

            if (GetLocalString(module, Prefix + "TOKEN") == GenerationToken)
                ctx.Skip("No reload since the last phase of this scenario ran.");

            var verified = Verify(ctx) + 1;
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", verified);
            if (verified >= ReloadsToVerify)
                Release();

            await ctx.WaitFrameAsync();
        }

        private static async Task PrepareAsync(EngineTestContext ctx)
        {
            var module = GetModule();
            var area = Area.GetAreaByResref(AreaResref);
            ctx.Assert(GetIsObjectValid(area), "the packed module contains the Abandoned Station");
            var landing = GetWaypointByTag(LandingTag);
            ctx.Assert(GetIsObjectValid(landing), "the station landing waypoint resolves");
            ctx.AssertEqual(0, Read(area).Active, "the station starts without active spawns");

            var areaContext = new EngineTestContext(ctx.TestName, area, GetLocation(landing));
            var fixture = await PlayerAbilityFixture.CreateAsync(areaContext);
            var creature = fixture.Creature;
            areaContext.Untrack(creature);
            fixture.KeepAcrossReloads();

            // The headless fixture does not raise the native area enter event, so the spawn handler runs as it would on entry.
            NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(area).AsNWSArea().m_oidLastEntered = creature;
            ScriptExecutionProvider.Current.ExecuteInScriptContext(Spawn.SpawnArea, area);
            await ctx.WaitUntilAsync(() => Read(area).Queued == 0 && Read(area).Active >= 3, 30f, "the module heartbeat places the area's queued spawns");

            // Three spawns are taken back to their respawn queue (minutes away), so the reload carries live and queued spawns.
            foreach (var spawned in Read(area).Objects.Take(3).ToList())
                ctx.Assert(Spawn.DespawnAndQueueRespawn(spawned), "a live spawn goes back to the respawn queue");
            await ctx.WaitFrameAsync();

            var now = Read(area);
            ctx.AssertEqual(3, now.Queued, "three spawns are queued");
            ctx.Assert(now.Active >= 3, $"spawns are live (active {now.Active})");
            ctx.AssertEqual(now.Active, now.Objects.Count, "every active spawn is one object in the area");
            ctx.AssertEqual(now.Active, now.SpawnIds, "no two objects share a spawn id");

            SetLocalObject(module, Prefix + "CREATURE", creature);
            SetLocalInt(module, Prefix + "CLIENT", (int)fixture.ClientId);
            SetLocalInt(module, Prefix + "WAS_PLAYER", fixture.WasPlayer);
            Remember(module, now);
            SetLocalInt(module, Prefix + "AREA_ALL", now.All);
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", 0);
            SetLocalInt(module, Prefix + "PREPARED", 1);
            ctx.SetResultDetail($"before the first reload: active={now.Active} queued={now.Queued} objects={now.Objects.Count} registered={now.All}");
        }

        private static int Verify(EngineTestContext ctx)
        {
            var module = GetModule();
            var area = Area.GetAreaByResref(AreaResref);
            var activeBefore = GetLocalInt(module, Prefix + "AREA_ACTIVE");
            var queuedBefore = GetLocalInt(module, Prefix + "AREA_QUEUED");
            var objectsBefore = GetLocalInt(module, Prefix + "COUNT");
            var now = Read(area);

            ctx.AssertEqual(GetLocalInt(module, Prefix + "AREA_ALL"), now.All, "the area's registered spawns come back from the carried registry");
            for (var index = 0; index < objectsBefore; index++)
            {
                var obj = GetLocalObject(module, Prefix + "OBJ" + index);
                ctx.Assert(GetIsObjectValid(obj), $"spawned object {index} survived the reload");
                ctx.Assert(now.ActiveObjects.Contains(obj), $"spawned object {index} is still an active spawn of the new registry");
            }

            ctx.AssertEqual(now.Active, now.Objects.Count, "every active spawn is exactly one object in the area (no area spawned twice)");
            ctx.AssertEqual(now.Active, now.SpawnIds, "no two objects share a spawn id");
            var spawnedInArea = new HashSet<uint>(now.Objects);
            ctx.Assert(now.ActiveObjects.SetEquals(spawnedInArea),
                $"the registry's active spawns are the spawned objects standing in the area; only in the registry: [{string.Join(", ", now.ActiveObjects.Except(spawnedInArea))}], only in the area: [{string.Join(", ", spawnedInArea.Except(now.ActiveObjects))}]");
            ctx.Assert(now.Objects.Count >= objectsBefore, $"no spawned object was lost ({now.Objects.Count} of {objectsBefore})");
            ctx.AssertEqual(activeBefore + queuedBefore, now.Active + now.Queued, "active plus queued spawns of the area are unchanged");

            ctx.SetResultDetail($"before: active={activeBefore} queued={queuedBefore} objects={objectsBefore}; after: active={now.Active} queued={now.Queued} objects={now.Objects.Count} registered={now.All}");
            Remember(module, now);
            return GetLocalInt(module, Prefix + "VERIFIED");
        }

        private static void Remember(uint module, AreaState now)
        {
            SetLocalInt(module, Prefix + "AREA_ACTIVE", now.Active);
            SetLocalInt(module, Prefix + "AREA_QUEUED", now.Queued);
            SetLocalInt(module, Prefix + "COUNT", now.Objects.Count);
            for (var index = 0; index < now.Objects.Count; index++)
                SetLocalObject(module, Prefix + "OBJ" + index, now.Objects[index]);
        }

        private sealed record AreaState(int Active, int Queued, int All, int SpawnIds, List<uint> Objects, HashSet<uint> ActiveObjects);

        private static AreaState Read(uint area)
        {
            var active = (IDictionary)SpawnInternals.Field("_activeSpawnsByArea");
            var queued = (IDictionary)SpawnInternals.Field("_queuedSpawnsByArea");
            var all = (IDictionary)SpawnInternals.Field("_allSpawnsByArea");
            var activeObjects = new HashSet<uint>();
            if (active.Contains(area))
            {
                foreach (var spawn in ((IList)active[area]).Cast<object>())
                    activeObjects.Add((uint)spawn.GetType().GetProperty("SpawnObject").GetValue(spawn));
            }

            var objects = new List<uint>();
            var ids = new HashSet<string>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                var id = GetLocalString(obj, "SPAWN_ID");
                if (id.Length == 0 || GetObjectType(obj) is not (ObjectType.Creature or ObjectType.Placeable))
                    continue;
                objects.Add(obj);
                ids.Add(id);
            }

            return new AreaState(
                active.Contains(area) ? ((IList)active[area]).Count : 0,
                queued.Contains(area) ? ((IList)queued[area]).Count : 0,
                all.Contains(area) ? ((IList)all[area]).Count : 0,
                ids.Count, objects, activeObjects);
        }

        /// <summary>The last phase: takes the player offline and removes the area's spawns the scenario caused.</summary>
        private static void Release()
        {
            var module = GetModule();
            var area = Area.GetAreaByResref(AreaResref);
            var creature = GetLocalObject(module, Prefix + "CREATURE");
            using (PlayerAbilityFixture.Reattach(creature, (uint)GetLocalInt(module, Prefix + "CLIENT"), GetLocalInt(module, Prefix + "WAS_PLAYER")))
            {
            }

            DestroyObject(creature);
            var active = (IDictionary)SpawnInternals.Field("_activeSpawnsByArea");
            if (active.Contains(area))
            {
                foreach (var spawn in ((IList)active[area]).Cast<object>().ToArray())
                    DestroyObject((uint)spawn.GetType().GetProperty("SpawnObject").GetValue(spawn));
                ((IList)active[area]).Clear();
            }

            var queued = (IDictionary)SpawnInternals.Field("_queuedSpawnsByArea");
            if (queued.Contains(area))
            {
                var queue = (IList)SpawnInternals.Field("_queuedSpawns");
                foreach (var pending in ((IList)queued[area]).Cast<object>().ToArray())
                    queue.Remove(pending);
                queued.Remove(area);
            }

            DeleteLocalObject(module, Prefix + "CREATURE");
            for (var index = 0; index < GetLocalInt(module, Prefix + "COUNT"); index++)
                DeleteLocalObject(module, Prefix + "OBJ" + index);
            foreach (var name in new[] { "CLIENT", "WAS_PLAYER", "VERIFIED", "PREPARED", "COUNT", "AREA_ACTIVE", "AREA_QUEUED", "AREA_ALL" })
                DeleteLocalInt(module, Prefix + name);
            DeleteLocalString(module, Prefix + "TOKEN");
        }
    }
}
