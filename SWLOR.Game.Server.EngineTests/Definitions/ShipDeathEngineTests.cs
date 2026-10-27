using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Space PvP rules and the death of a player ship (plan section 9) through the live pipeline. Two headless
    /// players stand in for two player ships. Nothing here needs a connected client.
    /// </summary>
    public static class ShipDeathEngineTests
    {
        private const string DockResref = "engine_pod_dock";

        private static ShipStatus Ship(int hull) => new()
        {
            Size = ShipSize.Medium,
            Hull = hull,
            MaxHull = 100,
            Shield = 0,
            MaxShield = 100,
            Capacitor = 100,
            MaxCapacitor = 100,
            HighPowerModules = { [1] = new ShipStatus.ShipStatusModule { ItemTag = "hull_rep_b" } },
        };

        [EngineTest("A player ship cannot damage another player's ship unless the ground rules allow it", Category = "ShipDeath", TimeoutSeconds = 60f)]
        public static async Task PvpGatesShipDamage(EngineTestContext ctx)
        {
            using var attacker = await PlayerAbilityFixture.CreateAsync(ctx);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, 2f);

            var attackerShip = "engine-pvp-attacker/" + attacker.Id;
            var targetShip = "engine-pvp-target/" + target.Id;
            attacker.Update(p => p.ActiveShipId = attackerShip);
            target.Update(p => p.ActiveShipId = targetShip);
            DB.Set(new PlayerShip { Id = attackerShip, OwnerPlayerId = attacker.Id, Status = Ship(100) });
            DB.Set(new PlayerShip { Id = targetShip, OwnerPlayerId = target.Id, Status = Ship(100) });
            var originalSetting = AreaPlugin.GetPVPSetting(ctx.Arena);

            try
            {
                int Hull() => DB.Get<PlayerShip>(targetShip).Status.Hull;

                AreaPlugin.SetPVPSetting(ctx.Arena, PvPSetting.FullPvP);
                SetPCLike(attacker.Creature, target.Creature);
                ctx.AssertEqual(SpacePvpDecision.NotHostile, Space.GetPvpDecision(attacker.Creature, target.Creature), "decision for a player not marked hostile");
                await ctx.ExecuteInCreatureContextAsync(attacker.Creature, () => Space.ApplyShipDamage(attacker.Creature, target.Creature, 10));
                ctx.AssertEqual(100, Hull(), "hull after an attack on a player who is not hostile");

                SetPCDislike(attacker.Creature, target.Creature);
                ctx.AssertEqual(SpacePvpDecision.Allowed, Space.GetPvpDecision(attacker.Creature, target.Creature), "decision for a hostile player in a full PvP area");
                ctx.Assert(GetIsReactionTypeHostile(target.Creature, attacker.Creature), "the ground check agrees");
                await ctx.ExecuteInCreatureContextAsync(attacker.Creature, () => Space.ApplyShipDamage(attacker.Creature, target.Creature, 10));
                ctx.AssertEqual(90, Hull(), "hull after an attack on a hostile player");

                AreaPlugin.SetPVPSetting(ctx.Arena, PvPSetting.NoPvP);
                ctx.AssertEqual(GetIsReactionTypeHostile(target.Creature, attacker.Creature), Space.CanShipAttack(attacker.Creature, target.Creature), "space follows the ground check in a no PvP area");
                await ctx.ExecuteInCreatureContextAsync(attacker.Creature, () => Space.ApplyShipDamage(attacker.Creature, target.Creature, 10));
                ctx.AssertEqual(Space.CanShipAttack(attacker.Creature, target.Creature) ? 80 : 90, Hull(), "hull after an attack in a no PvP area follows the ground check");

                // The ramp raises damage dealt to a player who has been in PvP combat for five minutes by 100%.
                AreaPlugin.SetPVPSetting(ctx.Arena, PvPSetting.FullPvP);
                PvpCombat.SetCombatSecondsForTesting(target.Creature, 300);
                var before = Hull();
                await ctx.ExecuteInCreatureContextAsync(attacker.Creature, () => Space.ApplyShipDamage(attacker.Creature, target.Creature, 10));
                ctx.AssertEqual(before - 20, Hull(), "hull after a hit during the 100% damage ramp");
            }
            finally
            {
                AreaPlugin.SetPVPSetting(ctx.Arena, originalSetting);
                PvpCombat.EndForTesting(target.Creature);
                PvpCombat.EndForTesting(attacker.Creature);
                SetPCLike(attacker.Creature, target.Creature);
                DB.Delete<PlayerShip>(attackerShip);
                DB.Delete<PlayerShip>(targetShip);
            }
        }

        [EngineTest("A destroyed ship ejects its pilot alive at the dock with the XP debt and keeps its modules", Category = "ShipDeath", TimeoutSeconds = 30f)]
        public static async Task DestroyedShipEjectsPilot(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);

            var shipId = "engine-pod-ship/" + pilot.Id;
            var propertyId = "engine-pod-property/" + pilot.Id;
            var dockArea = ctx.CreateInstancedArea(GetResRef(ctx.Arena));
            Area.GetAreas()[DockResref] = dockArea;
            var dock = GetPositionFromLocation(ctx.GetArenaLocation(5f));
            pilot.Update(p =>
            {
                p.ActiveShipId = shipId;
                p.OriginalAppearanceType = GetAppearanceType(pilot.Creature);
                p.SerializedHotBar = CreaturePlugin.SerializeQuickbar(pilot.Creature);
                p.TotalSPAcquired = 100;
                p.XPDebt = 0;
            });

            var property = new WorldProperty { Id = propertyId, OwnerPlayerId = pilot.Id };
            property.Positions[PropertyLocationType.DockPosition] = new PropertyLocation
            {
                AreaResref = DockResref, X = dock.X, Y = dock.Y, Z = dock.Z
            };
            DB.Set(property);
            DB.Set(new PlayerShip { Id = shipId, OwnerPlayerId = pilot.Id, PropertyId = propertyId, Status = Ship(0) });

            try
            {
                var ejected = false;
                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => ejected = Space.ApplyDeath(pilot.Creature));

                ctx.Assert(ejected, "the pilot of a destroyed ship is ejected alive");
                ctx.AssertEqual(System.Guid.Empty.ToString(), DB.Get<Player>(pilot.Id).ActiveShipId, "the pilot leaves space mode");
                ctx.Assert(DB.Get<Player>(pilot.Id).XPDebt > 0, "the pilot takes the normal ground XP debt");
                var ship = DB.Get<PlayerShip>(shipId).Status;
                ctx.AssertEqual(1, ship.Hull, "the wreck is left at 1 hull");
                ctx.AssertEqual(1, ship.HighPowerModules.Count, "modules stay installed outside Wild Space");
                await ctx.WaitUntilAsync(() => GetArea(pilot.Creature) == dockArea, 5, "the pod lands at the dock");
            }
            finally
            {
                DB.Delete<PlayerShip>(shipId);
                DB.Delete<WorldProperty>(propertyId);
            }
        }
    }
}
