using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Framework
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
        private readonly CancellationTokenSource _cancellation = new();
        private bool _seededRandom;

        public string TestName { get; }

        /// <summary>
        /// The instanced arena area this test runs in.
        /// </summary>
        public uint Arena { get; }

        /// <summary>
        /// Signaled by the runner when this test exceeds its timeout. The context's own wait
        /// helpers honor it automatically; tests that await NwTask directly should pass it along
        /// so a timed-out test stops promptly instead of running on into the next test.
        /// </summary>
        public CancellationToken CancellationToken => _cancellation.Token;

        /// <summary>
        /// Optional detail recorded by the test; becomes the result's message when the test
        /// passes, so information like per-case skip lists reaches the JSON report rather
        /// than only the log.
        /// </summary>
        public string ResultDetail { get; private set; }

        public void SetResultDetail(string detail)
        {
            ResultDetail = detail;
        }

        public EngineTestContext(string testName, uint arena, Location arenaSpawnLocation)
        {
            TestName = testName;
            Arena = arena;
            _arenaSpawnLocation = arenaSpawnLocation;
        }

        /// <summary>
        /// Requests cooperative cancellation of this test. Called by the runner on timeout.
        /// </summary>
        internal void CancelTest()
        {
            _cancellation.Cancel();
        }

        private void ThrowIfCancelled()
        {
            if (_cancellation.IsCancellationRequested)
                throw new OperationCanceledException($"Engine test '{TestName}' was cancelled by the runner (timeout).");
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
        /// The creature is normalized to the standard Defender faction regardless of its
        /// blueprint - stock blueprints vary (nw_rat001 ships as Hostile), and tests need a
        /// deterministic baseline where spawned creatures are friendly to each other and only
        /// MakeHostile creates an enemy. Fails the test if the creature could not be created.
        /// </summary>
        public uint SpawnCreature(string resref, float xOffset = 0f, float yOffset = 0f)
        {
            var creature = CreateObject(ObjectType.Creature, resref, GetArenaLocation(xOffset, yOffset));
            Assert(GetIsObjectValid(creature), $"Failed to spawn creature with resref '{resref}'.");
            ChangeToStandardFaction(creature, StandardFaction.Defender);
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
        /// Sets an NPC's current FP and Stamina pools. NPC max FP/STM come from skin item
        /// properties (zero for stock blueprints) plus stat adjustments, and both spawn
        /// initialization and heartbeat regen clamp the current-value locals to that max -
        /// so the max is raised via temporary stat modifiers first, then the pools are set.
        /// Call this AFTER the creature's spawn scripts have run (one frame after spawning),
        /// or spawn initialization will overwrite the pools with the unraised max.
        /// </summary>
        public void SetNPCResources(uint npc, int fp, int stamina)
        {
            TemporaryStatModifier.Add(npc, StatType.MaxFP, fp, 3600f, "ENGINE_TEST_RESOURCES");
            TemporaryStatModifier.Add(npc, StatType.MaxStamina, stamina, 3600f, "ENGINE_TEST_RESOURCES");

            // Pools start at EXACTLY the effective max (modifier + ability-score-derived),
            // which makes regen inert: at max, each regen tick clamps to no-op, so a cost
            // deduction is immediately visible as a dip below the pre-activation snapshot.
            // Starting below max, ~1/sec regen accrues during the activation delay and can
            // fully mask the deduction against that snapshot.
            SetLocalInt(npc, NPCCurrentFPVariable, Stat.GetMaxFP(npc));
            SetLocalInt(npc, NPCCurrentStaminaVariable, Stat.GetMaxStamina(npc));
        }

        /// <summary>
        /// Creates an item on a creature and equips it into the given slot, waiting for the
        /// action queue to actually perform the equip. Fails the test if the item can't be
        /// created or doesn't end up equipped. The item is destroyed with its owner at cleanup.
        /// </summary>
        public async Task<uint> EquipItemAsync(uint creature, string itemResref, InventorySlot slot, float timeoutSeconds = 10f)
        {
            // Item creation and equipping both need the creature's script context -
            // CreateItemOnObject returns OBJECT_INVALID when called from an async
            // continuation, even for stock blueprints. Creation and equipping run in
            // SEPARATE assigned contexts with a settle frame between: an ActionEquipItem
            // queued in the same script that created the item references an item that
            // hasn't finished entering the inventory and silently does nothing.
            var item = OBJECT_INVALID;
            AssignCommand(creature, () =>
            {
                item = CreateItemOnObject(itemResref, creature);
            });

            await WaitUntilAsync(
                () => GetIsObjectValid(item) && GetItemPossessor(item) == creature,
                timeoutSeconds,
                $"item '{itemResref}' to be created in the creature's inventory");
            await NwTask.NextFrame();

            // On a live server every player-equipped item gets the OnHitCastSpell property
            // from StandardItemConfigurations.AddOnHitProperty - but that handler bails for
            // non-PC equippers, so NPC fixtures must mirror it themselves. Without it, the
            // item_on_hit event never fires and queued weapon abilities can never consume.
            ApplyStandardOnHitProperty(item);

            // The equip validator cancels equips into an occupied slot (the swap path ends in
            // SkipEvent), so a caster that spawned armed must empty the hand first.
            var existing = GetItemInSlot(slot, creature);
            if (GetIsObjectValid(existing))
            {
                AssignCommand(creature, () =>
                {
                    ClearAllActions();
                    ActionUnequipItem(existing);
                });
                await WaitUntilAsync(
                    () => GetItemInSlot(slot, creature) != existing,
                    timeoutSeconds,
                    $"the previously equipped item to leave slot {slot}");
                await NwTask.NextFrame();
            }

            AssignCommand(creature, () =>
            {
                ClearAllActions();
                ActionEquipItem(item, slot);
            });
            await WaitUntilAsync(
                () => GetItemInSlot(slot, creature) == item,
                timeoutSeconds,
                $"item '{itemResref}' to be equipped in slot {slot}");

            return item;
        }

        /// <summary>
        /// Disables the NPC's natural regeneration: the out-of-combat 10%-per-tick HP heal
        /// AND the 1-per-tick FP/STM restore. Required for any test that wounds a creature
        /// and asserts a specific heal (a regen tick would satisfy the check for a broken
        /// impact) or verifies an exact resource cost (a regen tick would drift the pool
        /// off the exact post-deduction value).
        /// </summary>
        public void SuppressNPCNaturalRegen(uint npc)
        {
            SetLocalInt(npc, Stat.SuppressNaturalRegenVariable, 1);
        }

        /// <summary>
        /// Applies the OnHitCastSpell (Unique Power) item property the live equip pipeline
        /// adds to every player-equipped item (StandardItemConfigurations.AddOnHitProperty
        /// is PC-only). The item_on_hit script event this property fires is what consumes
        /// queued weapon abilities - an NPC wielding a weapon without it can queue but
        /// never consume.
        /// </summary>
        public void ApplyStandardOnHitProperty(uint item)
        {
            if (!GetIsObjectValid(item))
                return;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.OnHitCastSpell &&
                    GetItemPropertySubType(ip) == (int)OnHitCastSpell.ONHIT_UNIQUEPOWER)
                {
                    return;
                }
            }

            BiowareXP2.IPSafeAddItemProperty(
                item,
                ItemPropertyOnHitCastSpell(OnHitCastSpellType.ONHIT_UNIQUEPOWER, 40),
                0.0f,
                AddItemPropertyPolicy.ReplaceExisting,
                false,
                false);
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
            _seededRandom = true;
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
                ThrowIfCancelled();

                if (DateTime.UtcNow > deadline)
                    throw new EngineTestAssertionException($"Timed out after {timeoutSeconds}s waiting for: {description}");

                // NwTask completes silently when the token is cancelled; the next loop
                // iteration's ThrowIfCancelled turns that into a prompt exit.
                await NwTask.Delay(TimeSpan.FromMilliseconds(250), _cancellation.Token);
            }
        }

        /// <summary>
        /// Yields one server frame - e.g. so a freshly spawned creature's initialization
        /// scripts run before the test configures it. Observes runner cancellation so a
        /// timed-out test polling with repeated frame waits settles during the grace
        /// period instead of forcing a suite abort.
        /// </summary>
        public async Task WaitFrameAsync()
        {
            await NwTask.NextFrame();
            ThrowIfCancelled();
        }

        /// <summary>
        /// Waits for a fixed number of seconds of real server time.
        /// </summary>
        public async Task DelaySecondsAsync(float seconds)
        {
            await NwTask.Delay(TimeSpan.FromSeconds(seconds), _cancellation.Token);
            ThrowIfCancelled();
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
            if (_seededRandom)
            {
                Service.Random.ResetSeed();
                _seededRandom = false;
            }

            foreach (var obj in _trackedObjects)
            {
                if (!GetIsObjectValid(obj))
                    continue;

                // A fixture killed mid-test was marked non-destroyable by the death/loot
                // pipeline. Both calls run inside ONE assigned context: SetIsDestroyable
                // executes immediately there (it operates on OBJECT_SELF), and the
                // deferred destruction is processed after that same context ends -
                // guaranteeing the flag is restored first. A DestroyObject issued from
                // THIS context instead would run before the assigned callback and leave
                // the corpse in the shared arena.
                var target = obj;
                AssignCommand(target, () =>
                {
                    SetIsDestroyable(true, false, false);
                    DestroyObject(target);
                });
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
