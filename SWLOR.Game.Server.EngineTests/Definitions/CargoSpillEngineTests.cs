using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The trade core's cargo spill (plan section 13) through the live death pipeline: a destroyed ship's cargo lands
    /// in cargo pods, and another ship scoops what fits. Headless players stand in for the two ships.
    /// </summary>
    public static class CargoSpillEngineTests
    {
        private const string DockResref = "engine_cargo_dock";
        private const string FreighterTag = "sd_xsfreighter";

        [EngineTest("A destroyed ship spills its cargo into pods another ship can scoop", Category = "CargoSpill", TimeoutSeconds = 30f)]
        public static async Task DestroyedShipSpillsCargo(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            using var scooper = await PlayerAbilityFixture.CreateAsync(ctx, 2f);

            var shipId = "engine-spill-ship/" + pilot.Id;
            var scooperShipId = "engine-spill-scooper/" + scooper.Id;
            var propertyId = "engine-spill-property/" + pilot.Id;
            var dockArea = ctx.CreateInstancedArea(GetResRef(ctx.Arena));
            Area.GetAreas()[DockResref] = dockArea;
            var dock = GetPositionFromLocation(ctx.GetArenaLocation(5f));
            pilot.Update(p =>
            {
                p.ActiveShipId = shipId;
                p.OriginalAppearanceType = GetAppearanceType(pilot.Creature);
                p.SerializedHotBar = CreaturePlugin.SerializeQuickbar(pilot.Creature);
            });
            scooper.Update(p => p.ActiveShipId = scooperShipId);

            var property = new WorldProperty { Id = propertyId, OwnerPlayerId = pilot.Id };
            property.Positions[PropertyLocationType.DockPosition] = new PropertyLocation
            {
                AreaResref = DockResref, X = dock.X, Y = dock.Y, Z = dock.Z
            };
            DB.Set(property);

            var ship = new PlayerShip
            {
                Id = shipId, OwnerPlayerId = pilot.Id, PropertyId = propertyId,
                Status = new ShipStatus { ItemTag = FreighterTag, Size = ShipSize.Medium, MaxHull = 100, MaxShield = 100 }
            };
            CargoHoldRules.AddUnits(ship.Cargo, CommodityType.Grain, 200, 12000);
            CargoHoldRules.AddUnits(ship.Cargo, CommodityType.KinrathSilk, 30, 3000);
            DB.Set(ship);
            DB.Set(new PlayerShip
            {
                Id = scooperShipId, OwnerPlayerId = scooper.Id,
                Status = new ShipStatus { ItemTag = FreighterTag, Size = ShipSize.Medium, MaxHull = 100, MaxShield = 100 }
            });

            var pods = new System.Collections.Generic.List<uint>();
            try
            {
                var deathArea = GetArea(pilot.Creature);
                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => Space.ApplyDeath(pilot.Creature));

                ctx.AssertEqual(0, DB.Get<PlayerShip>(shipId).Cargo.Count, "the destroyed ship's cargo bay is empty");
                for (var pod = GetFirstObjectInArea(deathArea); GetIsObjectValid(pod); pod = GetNextObjectInArea(deathArea))
                {
                    if (GetResRef(pod) == Trade.CargoPodResref)
                        pods.Add(pod);
                }

                ctx.AssertEqual(2, pods.Count, "one pod per commodity");
                var spilled = pods.SelectMany(x => Trade.GetPodContents(x)).ToDictionary(x => x.Key, x => x.Value);
                ctx.AssertEqual(200, spilled[CommodityType.Grain], "the grain spilled");
                ctx.AssertEqual(30, spilled[CommodityType.KinrathSilk], "the silk spilled");

                // 200 grain (400 volume) and 30 silk fit in the scooper's 480-volume XS Freighter.
                foreach (var pod in pods)
                    Trade.ScoopPod(pod, scooper.Creature);

                var hold = DB.Get<PlayerShip>(scooperShipId).Cargo;
                ctx.AssertEqual(200, CargoHoldRules.GetUnits(hold, CommodityType.Grain), "the scooper took the grain");
                ctx.AssertEqual(30, CargoHoldRules.GetUnits(hold, CommodityType.KinrathSilk), "the scooper took the silk");
                ctx.AssertEqual(60 * 200, hold[CommodityType.Grain].CostBasis, "scooped goods cost their base price");
                await ctx.WaitUntilAsync(() => pods.All(x => !GetIsObjectValid(x)), 5, "empty pods are removed");
            }
            finally
            {
                foreach (var pod in pods.Where(GetIsObjectValid))
                    DestroyObject(pod);
                DB.Delete<PlayerShip>(shipId);
                DB.Delete<PlayerShip>(scooperShipId);
                DB.Delete<WorldProperty>(propertyId);
            }
        }
    }
}
