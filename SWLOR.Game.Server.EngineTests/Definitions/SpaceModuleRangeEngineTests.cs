using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpaceModuleRangeEngineTests
    {
        private const string FuelCapsule = "ship_fuelcapsule";
        private const string SpaceTarget = "SPACE_TARGET";
        private const int StartingPoints = 10;
        private const int StartingCapacitor = 100;

        [EngineTest("Far enemy target does not block a module that acts on the player's own ship", Category = "SpaceModuleRange", TimeoutSeconds = 60f)]
        public static async Task FarEnemyTarget_AllowsSelfModules(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var enemy = await SpawnFarShipAsync(ctx, player, "t2capital", 15f, true);

            try
            {
                // The reported case: an enemy capital ship is selected beyond the injector's range.
                var capsule = await GiveFuelCapsuleAsync(ctx, player);
                var status = await ActivateAsync(ctx, player, "cap_inject1", enemy);
                ctx.Assert(status.Capacitor > StartingCapacitor, $"the injector restores the player's capacitor ({status.Capacitor})");
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(capsule), 5f, "the injector to consume its fuel capsule");
                ctx.Assert(RecastStarted(status), "the injector starts its recast");

                status = await ActivateAsync(ctx, player, "shld_rep_1", enemy);
                ctx.Assert(status.Shield > StartingPoints, $"the shield repairer restores the player's shield ({status.Shield})");

                status = await ActivateAsync(ctx, player, "hull_rep_1", enemy);
                ctx.Assert(status.Hull > StartingPoints, $"the hull repairer restores the player's hull ({status.Hull})");

                // Pulses and the laser battery act around the player's ship and never read the selected target.
                foreach (var pulse in new[] { "repairfield", "bulwarkgen", "cap_ewar", "lasbattery1" })
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

        [EngineTest("Asteroid target falls back to the player's own ship for modules that can act on it", Category = "SpaceModuleRange", TimeoutSeconds = 60f)]
        public static async Task AsteroidTarget_FallsBackToOwnShip(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var asteroid = CreateObject(ObjectType.Placeable, "spc_asteroid", ctx.GetArenaLocation(4f, 0f));
            ctx.Assert(GetIsObjectValid(asteroid), "an asteroid exists");
            ctx.Track(asteroid);

            try
            {
                var capsule = await GiveFuelCapsuleAsync(ctx, player);
                var status = await ActivateAsync(ctx, player, "cap_inject1", asteroid);
                ctx.Assert(status.Capacitor > StartingCapacitor, $"the injector restores the player's capacitor ({status.Capacitor})");
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(capsule), 5f, "the injector to consume its fuel capsule");

                status = await ActivateAsync(ctx, player, "shld_rep_1", asteroid);
                ctx.Assert(status.Shield > StartingPoints, $"the shield repairer restores the player's shield ({status.Shield})");

                status = await ActivateAsync(ctx, player, "hull_rep_1", asteroid);
                ctx.Assert(status.Hull > StartingPoints, $"the hull repairer restores the player's hull ({status.Hull})");

                status = await ActivateAsync(ctx, player, "lasbattery1", asteroid);
                ctx.Assert(RecastStarted(status), "the laser battery starts its recast");

                // Targeted weapons still need a ship to fire at.
                status = await ActivateAsync(ctx, player, "turbolas1", asteroid);
                AssertRefused(ctx, status, "the turbolaser on an asteroid");
            }
            finally
            {
                RemoveShip(player);
            }
        }

        [EngineTest("Turbolaser with nothing selected is refused without spending anything", Category = "SpaceModuleRange", TimeoutSeconds = 60f)]
        public static async Task Turbolaser_NoTarget_IsRefused(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);

            try
            {
                var status = await ActivateAsync(ctx, player, "turbolas1", OBJECT_INVALID);
                AssertRefused(ctx, status, "the turbolaser with no target");
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
                var capsule = await GiveFuelCapsuleAsync(ctx, player);
                var status = await ActivateAsync(ctx, player, "cap_inject1", friend);
                ctx.AssertEqual(1, friendStatus.Capacitor, "out of range friend's capacitor");
                ctx.AssertEqual(StartingCapacitor, status.Capacitor, "player's capacitor after the refused injector");
                ctx.AssertEqual(player.Creature, GetItemPossessor(capsule), "fuel capsule holder after the refused injector");
                ctx.Assert(!RecastStarted(status), "the refused injector starts no recast");

                status = await ActivateAsync(ctx, player, "shld_rep_1", friend);
                AssertRefused(ctx, status, "the shield repairer on a far friend");

                status = await ActivateAsync(ctx, player, "hull_rep_1", friend);
                AssertRefused(ctx, status, "the hull repairer on a far friend");

                // Targeted weapons still need their selected target in range.
                status = await ActivateAsync(ctx, player, "turbolas1", enemy);
                AssertRefused(ctx, status, "the turbolaser on a far enemy");
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

        private static async Task<uint> GiveFuelCapsuleAsync(EngineTestContext ctx, PlayerAbilityFixture player)
        {
            var capsule = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => capsule = CreateItemOnObject(FuelCapsule, player.Creature));
            ctx.Assert(GetIsObjectValid(capsule), "a fuel capsule exists");
            await ctx.WaitUntilAsync(() => GetItemPossessor(capsule) == player.Creature, 5f, "the fuel capsule to enter the player's inventory");
            return capsule;
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
                    Capacitor = StartingCapacitor,
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

        private static void AssertRefused(EngineTestContext ctx, ShipStatus status, string use)
        {
            ctx.Assert(!RecastStarted(status), $"{use} starts no recast");
            ctx.AssertEqual(StartingCapacitor, status.Capacitor, $"capacitor after {use}");
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
