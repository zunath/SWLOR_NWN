using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.EngineTestService
{
    /// <summary>
    /// Per-test context passed to every [EngineTest] method.
    /// Provides arena access, creature spawning, NPC stat configuration,
    /// assertions, and async wait helpers. All objects spawned through this
    /// context are automatically destroyed when the test finishes.
    /// </summary>
    public class EngineTestContext
    {
        private const string NPCCurrentFPVariable = "FP";
        private const string NPCCurrentStaminaVariable = "STAMINA";

        private readonly List<uint> _trackedObjects = new();
        private readonly List<uint> _instancedAreas = new();
        private readonly Location _arenaSpawnLocation;

        public string TestName { get; }

        /// <summary>
        /// The instanced arena area this test runs in.
        /// </summary>
        public uint Arena { get; }

        public EngineTestContext(string testName, uint arena, Location arenaSpawnLocation)
        {
            TestName = testName;
            Arena = arena;
            _arenaSpawnLocation = arenaSpawnLocation;
        }

        /// <summary>
        /// Builds a location inside the arena, offset from the arena's known-walkable spawn point.
        /// </summary>
        public Location GetArenaLocation(float xOffset = 0f, float yOffset = 0f, float facing = 0f)
        {
            var position = GetPositionFromLocation(_arenaSpawnLocation);
            position.X += xOffset;
            position.Y += yOffset;

            return Location(Arena, position, facing);
        }

        /// <summary>
        /// Spawns a creature in the arena and tracks it for automatic cleanup.
        /// Fails the test if the creature could not be created.
        /// </summary>
        public uint SpawnCreature(string resref, float xOffset = 0f, float yOffset = 0f)
        {
            var creature = CreateObject(ObjectType.Creature, resref, GetArenaLocation(xOffset, yOffset));
            Assert(GetIsObjectValid(creature), $"Failed to spawn creature with resref '{resref}'.");
            Track(creature);

            return creature;
        }

        /// <summary>
        /// Registers an object for automatic destruction when the test completes.
        /// </summary>
        public void Track(uint obj)
        {
            _trackedObjects.Add(obj);
        }

        /// <summary>
        /// Creates an additional instanced copy of an area by resref and tracks it for cleanup.
        /// Fails the test if the area could not be created.
        /// </summary>
        public uint CreateInstancedArea(string areaResref)
        {
            var area = CreateArea(areaResref);
            Assert(GetIsObjectValid(area), $"Failed to create instanced area from resref '{areaResref}'.");
            _instancedAreas.Add(area);

            return area;
        }

        /// <summary>
        /// Caps an NPC's effective perk level. NPCs default to a perk's max level when this is unset.
        /// </summary>
        public void SetNPCPerkLevel(uint npc, PerkType perkType, int level)
        {
            SetLocalInt(npc, $"PERK_LEVEL_{(int)perkType}", level);
        }

        /// <summary>
        /// Sets an NPC's current FP and Stamina pools (stored as local variables for NPCs).
        /// </summary>
        public void SetNPCResources(uint npc, int fp, int stamina)
        {
            SetLocalInt(npc, NPCCurrentFPVariable, fp);
            SetLocalInt(npc, NPCCurrentStaminaVariable, stamina);
        }

        /// <summary>
        /// Moves a creature to the standard Hostile faction so other spawned creatures treat it as an enemy.
        /// </summary>
        public void MakeHostile(uint creature)
        {
            ChangeToStandardFaction(creature, StandardFaction.Hostile);
        }

        /// <summary>
        /// Reseeds the shared combat RNG so hit/crit/damage rolls are deterministic for this test.
        /// </summary>
        public void SeedRandom(int seed)
        {
            Service.Random.SetSeed(seed);
        }

        public void Assert(bool condition, string message)
        {
            if (!condition)
                throw new EngineTestAssertionException(message);
        }

        public void AssertEqual<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new EngineTestAssertionException($"{label}: expected '{expected}' but was '{actual}'.");
        }

        public void Fail(string message)
        {
            throw new EngineTestAssertionException(message);
        }

        public void Skip(string reason)
        {
            throw new EngineTestSkippedException(reason);
        }

        /// <summary>
        /// Polls a condition every quarter second until it becomes true or the timeout elapses.
        /// Fails the test on timeout.
        /// </summary>
        public async Task WaitUntilAsync(Func<bool> condition, float timeoutSeconds, string description)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                    throw new EngineTestAssertionException($"Timed out after {timeoutSeconds}s waiting for: {description}");

                await NwTask.Delay(TimeSpan.FromMilliseconds(250));
            }
        }

        /// <summary>
        /// Waits for a fixed number of seconds of real server time.
        /// </summary>
        public async Task DelaySecondsAsync(float seconds)
        {
            await NwTask.Delay(TimeSpan.FromSeconds(seconds));
        }

        public void Log(string message)
        {
            Service.Log.Write(LogGroup.EngineTest, $"[{TestName}] {message}", true);
        }

        /// <summary>
        /// Destroys all tracked objects and instanced areas. Called by the runner after each test.
        /// </summary>
        public void Cleanup()
        {
            foreach (var obj in _trackedObjects)
            {
                if (GetIsObjectValid(obj))
                    DestroyObject(obj);
            }
            _trackedObjects.Clear();

            foreach (var area in _instancedAreas)
            {
                if (GetIsObjectValid(area))
                    DestroyArea(area);
            }
            _instancedAreas.Clear();
        }
    }
}
