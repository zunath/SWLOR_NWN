using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>
    /// The station-only buildings (plan section 25): the Shipyard (Capital hulls are built only there), the Asteroid Drill
    /// (the anchor site's ore over time) and the Jump Beacon (a docked ship to another station's beacon for Station Fuel).
    /// Numbers: <see cref="StationTuning"/> (stations.py).
    /// </summary>
    public static class StationBuildingRules
    {
        private static readonly Dictionary<StructureType, StationBuildingDetail> _buildings =
            StationTuning.Buildings.ToDictionary(x => x.Structure);

        public static bool IsStationBuilding(StructureType structureType) => _buildings.ContainsKey(structureType);

        /// <summary>The building's detail, or null for anything else.</summary>
        public static StationBuildingDetail GetBuilding(StructureType structureType) =>
            _buildings.TryGetValue(structureType, out var building) ? building : null;

        /// <summary>Why a station-only building cannot be placed in this property, or an empty string.</summary>
        public static string GetPlacementError(StructureType structureType, bool isStation, bool inWildSpace)
        {
            var building = GetBuilding(structureType);
            if (building == null)
                return string.Empty;
            if (!isStation)
                return $"A {building.Name} can only be built on a player station's deck.";
            if (structureType == StructureType.JumpBeacon && inWildSpace)
                return "Jump Beacons cannot reach into or out of Wild Space.";

            return string.Empty;
        }

        // The Shipyard

        /// <summary>Why the player cannot build a Capital hull here, or <see cref="ShipyardRefusal.None"/>.</summary>
        public static ShipyardRefusal GetShipyardRefusal(bool atShipyard, ShipyardAccessType access, bool isCitizen, int owedTaxes)
        {
            if (!atShipyard)
                return ShipyardRefusal.NotAtShipyard;
            if (access != ShipyardAccessType.Anyone && !isCitizen)
                return ShipyardRefusal.CitizensOnly;
            if (isCitizen && owedTaxes > 0)
                return ShipyardRefusal.OwesTaxes;

            return ShipyardRefusal.None;
        }

        public static string DescribeShipyardRefusal(ShipyardRefusal refusal) => refusal switch
        {
            ShipyardRefusal.NotAtShipyard => "Capital hulls can only be built at a station Shipyard.",
            ShipyardRefusal.CitizensOnly => "Only this station's citizens may build at its Shipyard.",
            ShipyardRefusal.OwesTaxes => "You owe taxes to this station. Pay them at the Citizenship Terminal before using its Shipyard.",
            _ => string.Empty,
        };

        /// <summary>The Shipyard fee kept within 0 and the cap.</summary>
        public static int ClampShipyardFee(int fee) => Math.Clamp(fee, 0, StationTuning.ShipyardFeeMax);

        // The Asteroid Drill

        /// <summary>
        /// Ore units a day with the assigned droid's output percent (<see cref="StationDroidRules.DrillOutputPercent"/>; 0
        /// without a droid).
        /// </summary>
        public static double DrillUnitsPerDay(int droidPercent) =>
            StationTuning.DrillUnitsPerDay * (1 + Math.Max(0, droidPercent) / 100.0);

        /// <summary>The most ore the drill stores before it stops.</summary>
        public static int DrillCapacity(int droidPercent) =>
            (int)Math.Floor(DrillUnitsPerDay(droidPercent) * StationTuning.DrillStorageDays);

        /// <summary>
        /// The whole ore units mined since <paramref name="lastAccrual"/> and the time the next unit counts from. A drill that
        /// has never run starts now; a full drill mines nothing and counts from now, so it does not catch up once emptied.
        /// </summary>
        public static (int Units, DateTime NextAccrual) AccrueDrill(int storedUnits, DateTime lastAccrual, DateTime now, int droidPercent)
        {
            var capacity = DrillCapacity(droidPercent);
            if (lastAccrual == default || lastAccrual > now || storedUnits >= capacity)
                return (0, now);

            var hoursPerUnit = 24.0 / DrillUnitsPerDay(droidPercent);
            var units = (int)Math.Floor((now - lastAccrual).TotalHours / hoursPerUnit);
            if (units <= 0)
                return (0, lastAccrual);

            if (storedUnits + units >= capacity)
                return (capacity - storedUnits, now);

            return (units, lastAccrual.AddHours(units * hoursPerUnit));
        }

        /// <summary>The asteroid tables an anchor area's drill rolls, weighted as its asteroids spawn; empty for anything else.</summary>
        public static IReadOnlyList<(string LootTable, int Weight)> GetDrillTables(string areaResref) =>
            !string.IsNullOrWhiteSpace(areaResref) && StationTuning.DrillTables.TryGetValue(areaResref, out var tables)
                ? tables
                : Array.Empty<(string LootTable, int Weight)>();

        /// <summary>The drill table a roll from 0 to the total weight (exclusive) picks, or null when the area has none.</summary>
        public static string PickDrillTable(string areaResref, int roll)
        {
            foreach (var (table, weight) in GetDrillTables(areaResref))
            {
                if (roll < weight)
                    return table;
                roll -= weight;
            }

            return null;
        }

        public static int GetDrillTotalWeight(string areaResref) => GetDrillTables(areaResref).Sum(x => x.Weight);

        // The Jump Beacon

        public static int ClampBeaconLevel(int level) => Math.Clamp(level, 1, StationTuning.BeaconMaxLevel);

        /// <summary>The largest hull a beacon of this level jumps.</summary>
        public static ShipSize GetMaxHull(int level) => StationTuning.BeaconMaxHull[ClampBeaconLevel(level)];

        /// <summary>Whether both beacons take a hull of this size.</summary>
        public static bool TakesHull(int originLevel, int destinationLevel, ShipSize size) =>
            size != ShipSize.Invalid && (int)size <= (int)GetMaxHull(Math.Min(originLevel, destinationLevel));

        /// <summary>
        /// Station Fuel units a jump burns: by hull size, plus one per <see cref="StationTuning.JumpFuelCargoValuePerHop"/>
        /// credits of cargo value (<see cref="PirateAmbushRules.GetCargoValue"/>) per starlane hop between the two
        /// stations' systems, rounded up; less for the hulls built to save it (rounded up). The cargo's share keeps a jump
        /// from out-earning the starlanes it skips (plan section 25).
        /// </summary>
        public static int GetJumpFuel(ShipSize size, string hullTag, int cargoValue = 0, int hops = 0)
        {
            if (!StationTuning.JumpFuel.TryGetValue(size, out var units))
                return 0;

            units += GetCargoJumpFuel(cargoValue, hops);
            var discount = !string.IsNullOrWhiteSpace(hullTag) && StationTuning.JumpFuelDiscountPercent.TryGetValue(hullTag, out var percent)
                ? percent
                : 0;
            return Math.Max(1, (int)Math.Ceiling(units * (100 - discount) / 100.0));
        }

        /// <summary>
        /// The cargo value a jump carries (<see cref="PirateAmbushRules.GetCargoValue"/>): the ship's own cargo and sealed
        /// freight plus those of every ship docked aboard it, which jump with it.
        /// </summary>
        public static int GetJumpCargoValue(PlayerShip ship, IEnumerable<PlayerShip> dockedShips)
        {
            if (ship == null)
                return 0;

            var value = (long)PirateAmbushRules.GetCargoValue(ship.Cargo, ship.SealedFreight);
            foreach (var docked in (dockedShips ?? Enumerable.Empty<PlayerShip>()).Where(x => x != null && x.Id != ship.Id))
                value += PirateAmbushRules.GetCargoValue(docked.Cargo, docked.SealedFreight);

            return (int)Math.Min(int.MaxValue, value);
        }

        /// <summary>The cargo's share of a jump's fuel, before the hull's discount.</summary>
        public static int GetCargoJumpFuel(int cargoValue, int hops)
        {
            if (cargoValue <= 0 || hops <= 0)
                return 0;

            return (int)Math.Ceiling((long)cargoValue * hops / (double)StationTuning.JumpFuelCargoValuePerHop);
        }

        /// <summary>Station Fuel units this jump burns (<see cref="GetJumpFuel"/>).</summary>
        public static int GetJumpFuel(JumpRequest request) =>
            GetJumpFuel(request.ShipSize, request.HullTag, request.CargoValue, request.Hops);

        /// <summary>Whether a beacon's access setting admits the player.</summary>
        public static bool CanUseBeacon(JumpBeaconAccessType access, bool isCitizen, bool isAllyCitizen) => access switch
        {
            JumpBeaconAccessType.Anyone => true,
            JumpBeaconAccessType.Allies => isCitizen || isAllyCitizen,
            _ => isCitizen,
        };

        /// <summary>Why the beacon cannot jump the ship, or <see cref="JumpRefusal.None"/>.</summary>
        public static JumpRefusal GetJumpRefusal(JumpRequest request)
        {
            if (!request.ShipDocked)
                return JumpRefusal.ShipNotDocked;
            if (!request.CanPilot)
                return JumpRefusal.CannotPilot;
            if (request.OriginInWildSpace || request.DestinationInWildSpace)
                return JumpRefusal.WildSpace;
            if (request.SameStation)
                return JumpRefusal.SameStation;
            if (!request.OriginPermits)
                return JumpRefusal.OriginNotPermitted;
            if (!request.DestinationPermits)
                return JumpRefusal.DestinationNotPermitted;
            if (request.DestinationBlockaded)
                return JumpRefusal.DestinationBlockaded;
            if (request.ShipSize == ShipSize.Invalid)
                return JumpRefusal.UnknownSize;
            if (!TakesHull(request.OriginBeaconLevel, request.DestinationBeaconLevel, request.ShipSize))
                return JumpRefusal.HullTooLarge;
            if (request.FuelAboard < GetJumpFuel(request))
                return JumpRefusal.InsufficientFuel;

            return JumpRefusal.None;
        }

        public static string DescribeJumpRefusal(JumpRefusal refusal, JumpRequest request) => refusal switch
        {
            JumpRefusal.ShipNotDocked => "That ship is not docked at this station.",
            JumpRefusal.CannotPilot => "You do not have permission to pilot that ship.",
            JumpRefusal.WildSpace => "Jump Beacons cannot reach into or out of Wild Space.",
            JumpRefusal.SameStation => "The ship is already at this station.",
            JumpRefusal.OriginNotPermitted => "This station's Jump Beacon is not open to you.",
            JumpRefusal.DestinationNotPermitted => "That station's Jump Beacon does not accept you.",
            JumpRefusal.DestinationBlockaded => "An armada blockades that station's docks. Docking services there are offline until the blockade is broken.",
            JumpRefusal.UnknownSize => "This ship has no hull size, so no beacon can jump it. Notify an admin.",
            JumpRefusal.HullTooLarge => $"A beacon pair of level {Math.Min(request.OriginBeaconLevel, request.DestinationBeaconLevel)} jumps hulls up to {GetMaxHull(Math.Min(request.OriginBeaconLevel, request.DestinationBeaconLevel))}. Upgrade the beacons from Manage City.",
            JumpRefusal.InsufficientFuel => $"The jump needs {GetJumpFuel(request)} Station Fuel in the ship's cargo bay; it carries {request.FuelAboard}.",
            _ => string.Empty,
        };
    }
}
