using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Ship-to-ship docking (plan section 10) through the live database and combat state: a host finds its docked
    /// ships through the new search index, a ship whose host is gone is undocked, and threat or a landed hit puts a
    /// ship in combat. Docking itself moves the pilot between space and an interior, which needs a real client
    /// (release pass).
    /// </summary>
    public static class ShipDockingEngineTests
    {
        private const string EnemyResref = "nw_rat001";

        [EngineTest("A host finds the ships docked aboard it; a ship whose host is gone is undocked", Category = "ShipDocking", TimeoutSeconds = 60f)]
        public static async Task HostsFindTheirDockedShips(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();
            var suffix = Guid.NewGuid().ToString("N");
            var hostId = "engine-dock-host/" + suffix;
            var firstId = "engine-dock-first/" + suffix;
            var secondId = "engine-dock-second/" + suffix;
            var strayId = "engine-dock-stray/" + suffix;

            try
            {
                DB.Set(new PlayerShip { Id = hostId, OwnerPlayerId = "host-owner", Status = new ShipStatus { Size = ShipSize.Capital } });
                DB.Set(new PlayerShip { Id = firstId, OwnerPlayerId = "first-owner", DockedToShipId = hostId, Status = new ShipStatus() });
                DB.Set(new PlayerShip { Id = secondId, OwnerPlayerId = "second-owner", DockedToShipId = hostId, Status = new ShipStatus(),
                    DockedCrewPlayerIds = new List<string> { "second-owner" } });
                DB.Set(new PlayerShip { Id = strayId, OwnerPlayerId = "stray-owner", DockedToShipId = "engine-dock-missing/" + suffix, Status = new ShipStatus() });

                var docked = Space.GetDockedShips(hostId);
                ctx.AssertEqual(2, docked.Count, "ships docked aboard the host");
                ctx.Assert(docked.Exists(x => x.Id == firstId) && docked.Exists(x => x.Id == secondId), "both docked ships are found");
                ctx.AssertEqual(0, Space.GetDockedShips(firstId).Count, "ships docked aboard a visitor");

                ctx.Assert(Space.IsDockedAboardHost(DB.Get<PlayerShip>(firstId), out _), "a ship whose host exists is docked");
                ctx.Assert(!Space.IsDockedAboardHost(DB.Get<PlayerShip>(strayId), out _), "a ship whose host is gone is not docked");
                ctx.AssertEqual(string.Empty, DB.Get<PlayerShip>(strayId).DockedToShipId, "the stray ship's saved host");
            }
            finally
            {
                DB.Delete<PlayerShip>(hostId);
                DB.Delete<PlayerShip>(firstId);
                DB.Delete<PlayerShip>(secondId);
                DB.Delete<PlayerShip>(strayId);
            }
        }

        [EngineTest("Threat on a ship puts it in combat for docking", Category = "ShipDocking", TimeoutSeconds = 60f)]
        public static async Task ThreatPutsAShipInCombat(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            ctx.Assert(!Space.IsShipInCombat(pilot.Creature), "a ship nobody fights is not in combat");

            var enemy = ctx.SpawnCreature(EnemyResref, 6f);
            await ctx.WaitFrameAsync();
            try
            {
                Enmity.ModifyEnmity(pilot.Creature, enemy, 100);
                ctx.Assert(Space.IsShipInCombat(pilot.Creature), "a ship an enemy holds threat on is in combat");
            }
            finally
            {
                Enmity.RemoveCreatureEnmity(pilot.Creature);
            }
        }
    }
}
