using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpaceModuleRangeEngineTests
    {
        private const string FuelCapsule = "ship_fuelcapsule";
        private const string SpaceTarget = "SPACE_TARGET";
        private const int StartingPoints = 10;

        [EngineTest("Far enemy target does not block a module that acts on the player's own ship", Category = "SpaceModuleRange", TimeoutSeconds = 60f)]
        public static async Task FarEnemyTarget_AllowsSelfModules(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var enemy = await SpawnFarShipAsync(ctx, player, "t2capital", 15f, true);

            try
            {
                // The reported case: an enemy capital ship is selected beyond the injector's range.
                await GiveFuelCapsuleAsync(ctx, player);
                var status = await ActivateAsync(ctx, player, "cap_inject1", enemy);
                ctx.Assert(status.Capacitor > StartingPoints, $"the injector restores the player's capacitor ({status.Capacitor})");
                ctx.Assert(!GetIsObjectValid(GetItemPossessedBy(player.Creature, FuelCapsule)), "the injector consumes its fuel capsule");
                ctx.Assert(RecastStarted(status), "the injector starts its recast");

                status = await ActivateAsync(ctx, player, "shld_rep_1", enemy);
                ctx.Assert(status.Shield > StartingPoints, $"the shield repairer restores the player's shield ({status.Shield})");

                status = await ActivateAsync(ctx, player, "hull_rep_1", enemy);
                ctx.Assert(status.Hull > StartingPoints, $"the hull repairer restores the player's hull ({status.Hull})");

                // Pulses around the player's ship never read the selected target.
                foreach (var pulse in new[] { "repairfield", "bulwarkgen", "cap_ewar" })
                {
                    status = await ActivateAsync(ctx, player, pulse, enemy);
                    ctx.Assert(RecastStarted(status), $"{pulse} starts its recast");
                }
            }
            finally
            {
                RemoveShip(player);
            }
        }

        [EngineTest("Range rules stay for friendly targets and targeted weapons", Category = "SpaceModuleRange", TimeoutSeconds = 60f)]
        public static async Task RangeRules_StayForFriendsAndWeapons(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var friend = await SpawnFarShipAsync(ctx, player, "t2cargo", 5f, false);
            var enemy = await SpawnFarShipAsync(ctx, player, "t2capital", 15f, true);

            try
            {
                // A friendly ship beyond range receives nothing, and nothing is spent.
                var friendStatus = Space.GetShipStatus(friend);
                friendStatus.Capacitor = 1;
                await GiveFuelCapsuleAsync(ctx, player);
                var status = await ActivateAsync(ctx, player, "cap_inject1", friend);
                ctx.AssertEqual(1, friendStatus.Capacitor, "out of range friend's capacitor");
                ctx.AssertEqual(StartingPoints, status.Capacitor, "player's capacitor after the refused injector");
                ctx.Assert(GetIsObjectValid(GetItemPossessedBy(player.Creature, FuelCapsule)), "the refused injector keeps its fuel capsule");
                ctx.Assert(!RecastStarted(status), "the refused injector starts no recast");

                status = await ActivateAsync(ctx, player, "shld_rep_1", friend);
                ctx.Assert(!RecastStarted(status), "the shield repairer is refused on a far friend");

                status = await ActivateAsync(ctx, player, "hull_rep_1", friend);
                ctx.Assert(!RecastStarted(status), "the hull repairer is refused on a far friend");

                // Self-capable weapons still need their selected target in range.
                status = await ActivateAsync(ctx, player, "turbolas1", enemy);
                ctx.Assert(!RecastStarted(status), "the turbolaser is refused on a far enemy");

                status = await ActivateAsync(ctx, player, "lasbattery1", enemy);
                ctx.Assert(!RecastStarted(status), "the laser battery is refused on a far enemy");
            }
            finally
            {
                RemoveShip(player);
            }
        }

        private static async Task<uint> SpawnFarShipAsync(EngineTestContext ctx, PlayerAbilityFixture player, string resref, float yOffset, bool hostile)
        {
            var ship = ctx.SpawnCreature(resref, 35f, yOffset);
            ApplyEffectToObject(DurationType.Permanent, EffectCutsceneImmobilize(), ship);
            await ctx.WaitUntilAsync(() => Space.GetShipStatus(ship) != null, 5f, $"{resref} to register as an NPC ship");

            if (hostile)
                ctx.MakeHostile(ship);

            ctx.AssertEqual(hostile, GetIsEnemy(ship, player.Creature), $"{resref} is an enemy of the player");
            ctx.Assert(GetDistanceBetween(player.Creature, ship) > 30f,
                $"{resref} is beyond every tested module's range ({GetDistanceBetween(player.Creature, ship):0.0}m)");
            return ship;
        }

        private static async Task GiveFuelCapsuleAsync(EngineTestContext ctx, PlayerAbilityFixture player)
        {
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => CreateItemOnObject(FuelCapsule, player.Creature));
            ctx.Assert(GetIsObjectValid(GetItemPossessedBy(player.Creature, FuelCapsule)), "the player holds a fuel capsule");
        }

        /// <summary>
        /// Fits a fresh ship with only the module and uses it through the real activation path with the target selected.
        /// </summary>
        private static async Task<ShipStatus> ActivateAsync(EngineTestContext ctx, PlayerAbilityFixture player, string itemTag, uint target)
        {
            RemoveShip(player);

            var ship = new PlayerShip
            {
                OwnerPlayerId = player.Id,
                Status = new ShipStatus
                {
                    ItemTag = "ShipDeedLightFreighter",
                    Hull = StartingPoints,
                    MaxHull = 500,
                    Shield = StartingPoints,
                    MaxShield = 500,
                    Capacitor = StartingPoints,
                    MaxCapacitor = 500,
                }
            };
            ship.Status.HighPowerModules[1] = new ShipStatus.ShipStatusModule
            {
                ItemTag = itemTag,
                RecastTime = DateTime.UtcNow.AddSeconds(-1)
            };
            DB.Set(ship);
            player.Update(record => record.ActiveShipId = ship.Id);

            SetLocalObject(player.Creature, SpaceTarget, target);
            await ctx.ExecuteInCreatureContextAsync(player.Creature,
                () => Space.ActivateShipModule(player.Creature, Space.HighSlotToFeat(1)));
            return Space.GetShipStatus(player.Creature);
        }

        private static bool RecastStarted(ShipStatus status)
        {
            return status.HighPowerModules[1].RecastTime > DateTime.UtcNow;
        }

        private static void RemoveShip(PlayerAbilityFixture player)
        {
            var shipId = DB.Get<Player>(player.Id).ActiveShipId;
            if (string.IsNullOrEmpty(shipId) || shipId == Guid.Empty.ToString())
                return;

            player.Update(record => record.ActiveShipId = Guid.Empty.ToString());
            DB.Delete<PlayerShip>(shipId);
        }
    }
}
