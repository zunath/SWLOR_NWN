using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpaceModuleRangeEngineTests
    {
        private const string FuelCapsule = "ship_fuelcapsule";
        private const string SpaceTarget = "SPACE_TARGET";
        private const int StartingPoints = 10;
        private const int StartingCapacitor = 100;
        private const string Injector = "sm_hyper_00";
        private const string ShieldRepairer = "sm_shrep_00";
        private const string HullRepairer = "sm_hrep_00";
        private const string RepairField = "sm_rfield_40";
        private const string Bulwark = "sm_bulw_40";
        private const string EWarArray = "sm_ewar_40";
        private const string Turbolaser = "sm_turbo_40";

        [EngineTest("Far enemy target does not block a module that acts on the player's own ship", Category = "SpaceModuleRange", TimeoutSeconds = 60f)]
        public static async Task FarEnemyTarget_AllowsSelfModules(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var enemy = await SpawnFarShipAsync(ctx, player, "t2capital", 15f, true);

            try
            {
                // The reported case: an enemy capital ship is selected far away.
                var capsule = await GiveFuelCapsuleAsync(ctx, player);
                var status = await ActivateAsync(ctx, player, Injector, enemy);
                ctx.Assert(status.Capacitor > StartingCapacitor, $"the injector restores the player's capacitor ({status.Capacitor})");
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(capsule), 5f, "the injector to consume its fuel capsule");
                ctx.Assert(RecastStarted(status), "the injector starts its recast");

                status = await ActivateAsync(ctx, player, ShieldRepairer, enemy);
                ctx.Assert(status.Shield > StartingPoints, $"the shield repairer restores the player's shield ({status.Shield})");

                status = await ActivateAsync(ctx, player, HullRepairer, enemy);
                ctx.Assert(status.Hull > StartingPoints, $"the hull repairer restores the player's hull ({status.Hull})");

                // Pulses act around the player's ship and never read the selected target.
                foreach (var pulse in new[] { RepairField, Bulwark, EWarArray })
                {
                    status = await ActivateAsync(ctx, player, pulse, enemy);
                    ctx.Assert(Used(status), $"{pulse} switches on");
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
                var status = await ActivateAsync(ctx, player, Injector, asteroid);
                ctx.Assert(status.Capacitor > StartingCapacitor, $"the injector restores the player's capacitor ({status.Capacitor})");
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(capsule), 5f, "the injector to consume its fuel capsule");

                status = await ActivateAsync(ctx, player, ShieldRepairer, asteroid);
                ctx.Assert(status.Shield > StartingPoints, $"the shield repairer restores the player's shield ({status.Shield})");

                status = await ActivateAsync(ctx, player, HullRepairer, asteroid);
                ctx.Assert(status.Hull > StartingPoints, $"the hull repairer restores the player's hull ({status.Hull})");

                // Targeted weapons still need a ship to fire at.
                status = await ActivateAsync(ctx, player, Turbolaser, asteroid);
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
                var status = await ActivateAsync(ctx, player, Turbolaser, OBJECT_INVALID);
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
                // A friendly ship beyond range receives no repair, and nothing is spent.
                var status = await ActivateAsync(ctx, player, ShieldRepairer, friend);
                AssertRefused(ctx, status, "the shield repairer on a far friend");

                status = await ActivateAsync(ctx, player, HullRepairer, friend);
                AssertRefused(ctx, status, "the hull repairer on a far friend");

                // Targeted weapons still need their selected target in range.
                status = await ActivateAsync(ctx, player, Turbolaser, enemy);
                AssertRefused(ctx, status, "the turbolaser on a far enemy");
            }
            finally
            {
                RemoveShip(player);
            }
        }

        private static async Task<uint> SpawnFarShipAsync(EngineTestContext ctx, PlayerAbilityFixture player, string resref, float yOffset, bool hostile)
        {
            var ship = ctx.SpawnCreature(resref, 70f, yOffset);
            ApplyEffectToObject(DurationType.Permanent, EffectCutsceneImmobilize(), ship);
            await ctx.WaitUntilAsync(() => Space.GetShipStatus(ship) != null, 5f, $"{resref} to register as an NPC ship");

            if (hostile)
                ctx.MakeHostile(ship);

            ctx.AssertEqual(hostile, GetIsEnemy(ship, player.Creature), $"{resref} is an enemy of the player");
            ctx.Assert(GetDistanceBetween(player.Creature, ship) > 60f,
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
            var status = Space.GetShipStatus(player.Creature);
            // Repairers and pulses are toggles: note whether this one switched on, then keep it to its first cycle.
            _toggledOn = Space.IsShipToggleOn(status, 1);
            Space.TurnOffShipToggles(player.Creature);
            return status;
        }

        private static bool _toggledOn;

        private static void AssertRefused(EngineTestContext ctx, ShipStatus status, string use)
        {
            ctx.Assert(!Used(status), $"{use} neither fires nor switches on");
            ctx.AssertEqual(StartingCapacitor, status.Capacitor, $"capacitor after {use}");
        }

        private static bool Used(ShipStatus status) => RecastStarted(status) || _toggledOn;

        private static bool RecastStarted(ShipStatus status)
        {
            return status.HighPowerModules[1].RecastTime > DateTime.UtcNow;
        }

        private static void RemoveShip(PlayerAbilityFixture player)
        {
            var shipId = DB.Get<Player>(player.Id).ActiveShipId;
            if (string.IsNullOrEmpty(shipId) || shipId == Guid.Empty.ToString())
                return;

            Space.TurnOffShipToggles(player.Creature);
            AbilityCooldownVisual.ClearAllRecastDelays(player.Creature);
            player.Update(record => record.ActiveShipId = Guid.Empty.ToString());
            DB.Delete<PlayerShip>(shipId);
        }
    }
}
