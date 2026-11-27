using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The strip miner pays out when its cycle ends. Production logged the payout looking up an empty loot table
    /// because the asteroid had despawned in the meantime; a missing asteroid must yield nothing.
    /// </summary>
    public static class StripMinerEngineTests
    {
        private const string Category = "StripMiner";
        private const string StripMinerTag = "sm_strip_00";
        private const string Asteroid = "spc_asteroid_til";
        private const string ControlAsteroid = "asteroid_arda";

        [EngineTest("A strip-mined asteroid that still exists is depleted and yields ore", Category = Category, TimeoutSeconds = 40f)]
        public static async Task PresentAsteroidYieldsOre(EngineTestContext ctx)
        {
            using var actor = await PlayerAbilityFixture.CreateAsync(ctx);
            var shipId = CreateShip(actor);
            try
            {
                var asteroid = SpawnAsteroid(ctx, Asteroid);
                var ores = StripOres(asteroid);

                await StartStripMiningAsync(ctx, actor.Creature, asteroid);
                await ctx.WaitUntilAsync(() => CountOre(actor.Creature, ores) > 0, 25f, "the strip miner pays out ore");

                await ctx.WaitUntilAsync(() => !GetIsObjectValid(asteroid) || GetIsDead(asteroid), 5f, "the asteroid is depleted");
            }
            finally { DB.Delete<PlayerShip>(shipId); }
        }

        [EngineTest("A strip-mined asteroid that is gone when mining finishes yields nothing", Category = Category, TimeoutSeconds = 40f)]
        public static async Task MissingAsteroidYieldsNothing(EngineTestContext ctx)
        {
            using var actor = await PlayerAbilityFixture.CreateAsync(ctx);
            var shipId = CreateShip(actor);
            try
            {
                var asteroid = SpawnAsteroid(ctx, Asteroid);
                // A second, intact asteroid of another ore, stripped at the same time, proves the delayed payout ran.
                // It is started directly so the tier 1 strip miner's tier check does not refuse it.
                var control = SpawnAsteroid(ctx, ControlAsteroid);
                var ores = StripOres(asteroid);
                var controlOres = StripOres(control);
                ctx.Assert(!ores.Overlaps(controlOres), "the two asteroids yield different ore");

                await StartStripMiningAsync(ctx, actor.Creature, asteroid);
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => ShipModuleActions.StripAsteroid(actor.Creature, control, 10f, 0));
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => DestroyObject(asteroid));
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(asteroid), 5f, "the asteroid is destroyed mid-mine");

                await ctx.WaitUntilAsync(() => CountOre(actor.Creature, controlOres) > 0, 25f, "the intact asteroid pays out");
                await ctx.DelaySecondsAsync(2f);
                ctx.AssertEqual(0, CountOre(actor.Creature, ores), "the destroyed asteroid yields no ore");
            }
            finally { DB.Delete<PlayerShip>(shipId); }
        }

        private static string CreateShip(PlayerAbilityFixture actor)
        {
            var shipId = "engine-strip-miner/" + actor.Id;
            // A strip miner needs a Large or Capital hull. The ship has no property, so its yield goes to the pilot's inventory.
            var status = new ShipStatus { Size = ShipSize.Large, Hull = 100, MaxHull = 100, Capacitor = 100, MaxCapacitor = 100 };
            status.HighPowerModules[1] = new ShipStatus.ShipStatusModule { ItemTag = StripMinerTag };
            actor.Update(p => p.ActiveShipId = shipId);
            DB.Set(new PlayerShip { Id = shipId, OwnerPlayerId = actor.Id, Status = status });
            return shipId;
        }

        private static uint SpawnAsteroid(EngineTestContext ctx, string resref)
        {
            var asteroid = CreateObject(ObjectType.Placeable, resref, ctx.GetArenaLocation(2f));
            ctx.Track(asteroid);
            ctx.Assert(GetIsObjectValid(asteroid), $"spawn {resref}");
            ctx.Assert(!string.IsNullOrWhiteSpace(GetLocalString(asteroid, "STRIPMINE_LOOT_TABLE_ID")), $"{resref} can be strip mined");
            return asteroid;
        }

        /// <summary>Starts the strip miner in the pilot's script context, where its delayed payout can be scheduled.</summary>
        private static async Task StartStripMiningAsync(EngineTestContext ctx, uint activator, uint asteroid)
        {
            var module = new RevampModuleDefinition().BuildShipModules()[StripMinerTag];
            var status = Space.GetShipStatus(activator);
            ctx.AssertEqual(string.Empty, module.ModuleValidationAction(activator, status, asteroid, null, 0), "the strip miner accepts the asteroid");
            await ctx.ExecuteInCreatureContextAsync(activator, () => module.ModuleActivatedAction(activator, status, asteroid, null, 0));
        }

        private static HashSet<string> StripOres(uint asteroid) =>
            Loot.GetLootTableByName(GetLocalString(asteroid, "STRIPMINE_LOOT_TABLE_ID")).Select(item => item.Resref).ToHashSet();

        private static int CountOre(uint creature, HashSet<string> ores)
        {
            var count = 0;
            for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
                if (ores.Contains(GetResRef(item)))
                    count += GetItemStackSize(item);
            return count;
        }
    }
}
