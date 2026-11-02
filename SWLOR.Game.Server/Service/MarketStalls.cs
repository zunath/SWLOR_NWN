using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The city Market Stalls (Player City Revamp, "Living cities"): a city places one stall for each city level, each stall
    /// gets a vendor selling the stock theme the mayor picked, and a city that drops a level closes its newest stalls above
    /// the limit (the stall stays standing, with no vendor). The rules live in <see cref="MarketStallRules"/>; the stock
    /// comes from <see cref="CityStores"/>.
    /// </summary>
    public static class MarketStalls
    {
        private const string VendorVariable = "MARKET_STALL_VENDOR";
        private const string StoreVariable = "MARKET_STALL_STORE";

        /// <summary>The structure id of the stall a vendor tends, kept on the vendor.</summary>
        private const string StallIdVariable = "MARKET_STALL_ID";

        /// <summary>The structure id of the stall a vendor tends, or an empty string.</summary>
        public static string GetStallId(uint vendor)
        {
            return GetIsObjectValid(vendor) ? GetLocalString(vendor, StallIdVariable) : string.Empty;
        }

        /// <summary>
        /// A stall was placed, moved or loaded: its vendor is rebuilt where the stall stands now, and the city's stalls
        /// open or close for its level.
        /// </summary>
        public static void OnStallPlaced(WorldProperty stall, uint placeable)
        {
            Despawn(placeable);

            if (stall.MarketStallTheme == MarketStallTheme.Invalid)
            {
                stall.MarketStallTheme = MarketStallTheme.GeneralGoods;
                DB.Set(stall);
            }

            Refresh(stall.ParentPropertyId);
        }

        /// <summary>A stall was picked up: its vendor goes with it, and a closed stall may take its place.</summary>
        public static void OnStallRetrieved(WorldProperty stall, uint placeable)
        {
            Despawn(placeable);
            Refresh(stall.ParentPropertyId, stall.Id);
        }

        /// <summary>
        /// Opens the oldest stalls the city's level allows, each with its vendor, and closes the rest. Safe to call at any
        /// time: a stall that is already in the right state is left alone, and a stall whose placeable is not in the world
        /// yet is handled when it spawns.
        /// </summary>
        /// <param name="cityId">The city (or station).</param>
        /// <param name="ignoredStallId">A stall that is being removed.</param>
        public static void Refresh(string cityId, string ignoredStallId = "")
        {
            var city = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);
            if (city == null || city.IsQueuedForDeletion)
                return;

            var stalls = GetStalls(cityId).Where(x => x.Id != ignoredStallId).ToList();
            var level = city.Upgrades.GetValueOrDefault(PropertyUpgradeType.CityLevel, 1);
            var open = MarketStallRules.SelectOpenStalls(stalls.Select(x => (x.Id, x.DateCreated)), level);

            foreach (var stall in stalls)
            {
                var placeable = Property.GetPlaceableByPropertyId(stall.Id);
                if (!GetIsObjectValid(placeable))
                    continue;

                var isOpen = open.Contains(stall.Id);
                SetName(placeable, MarketStallRules.GetStallName(stall.CustomName, isOpen));

                if (!isOpen)
                    Despawn(placeable);
                else if (!GetIsObjectValid(GetLocalObject(placeable, VendorVariable)))
                    SpawnVendor(city, stall, placeable);
            }
        }

        /// <summary>The stalls a city (or station) has, whether they are open or closed.</summary>
        public static List<WorldProperty> GetStalls(string cityId)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), cityId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                .AddFieldSearch(nameof(WorldProperty.StructureType), (int)StructureType.MarketStall);
            var count = checked((int)DB.SearchCount(query));

            return count <= 0
                ? new List<WorldProperty>()
                : DB.Search(query.AddPaging(count, 0)).Where(x => !x.IsQueuedForDeletion).ToList();
        }

        /// <summary>How many stalls a city has, so a placement can be refused at the limit.</summary>
        public static int CountStalls(string cityId)
        {
            return GetStalls(cityId).Count;
        }

        /// <summary>Whether this player may pick a stall's stock theme: the mayor, or anyone who may edit the city's structures.</summary>
        public static bool CanPickTheme(uint player, WorldProperty city)
        {
            if (city == null || !GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            if (city.OwnerPlayerId == playerId)
                return true;

            var permission = DB.Search(new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), city.Id, false))
                .FirstOrDefault();

            return permission != null && permission.Permissions.GetValueOrDefault(PropertyPermissionType.EditStructures);
        }

        /// <summary>
        /// Sets what a stall sells and replaces its vendor and store. Returns an empty string when it worked, otherwise
        /// what stopped it.
        /// </summary>
        public static string ChangeTheme(uint player, string stallId, MarketStallTheme theme)
        {
            var stall = string.IsNullOrWhiteSpace(stallId) ? null : DB.Get<WorldProperty>(stallId);
            if (stall == null || stall.StructureType != StructureType.MarketStall || stall.IsQueuedForDeletion)
                return "This stall is closed.";

            var city = DB.Get<WorldProperty>(stall.ParentPropertyId);
            if (!CanPickTheme(player, city))
                return "Only the mayor, or someone who may edit the city's structures, can change what a stall sells.";

            stall.MarketStallTheme = MarketStallRules.NormalizeTheme(theme);
            DB.Set(stall);
            Log.Write(LogGroup.Property, $"Market stall '{stall.CustomName}' ({stall.Id}) of city '{city.CustomName}' ({city.Id}) now sells {stall.MarketStallTheme}.");

            var placeable = Property.GetPlaceableByPropertyId(stall.Id);
            Despawn(placeable);
            Refresh(city.Id);

            return string.Empty;
        }

        private static void SpawnVendor(WorldProperty city, WorldProperty stall, uint placeable)
        {
            var area = GetArea(placeable);
            var position = GetPosition(placeable);
            var placement = MarketStallRules.GetVendorPlacement(position.X, position.Y, GetFacing(placeable));
            var location = Location(area, Vector3(placement.X, placement.Y, position.Z), placement.FacingDegrees);

            var vendor = CreateObject(ObjectType.Creature, "cs_stallvendor", location);
            if (!GetIsObjectValid(vendor))
            {
                Log.Write(LogGroup.Error, $"The vendor of market stall '{stall.CustomName}' ({stall.Id}) could not be created. Check that 'cs_stallvendor' is in the module.", true);
                return;
            }

            SetPlotFlag(vendor, true);
            SetLocalString(vendor, StallIdVariable, stall.Id);

            var kind = CityStoreRules.GetKindForTheme(stall.MarketStallTheme);
            var store = CityStores.Create(city, kind, location);
            if (GetIsObjectValid(store))
                SetLocalObject(vendor, StoreVariable, store);

            SetLocalObject(placeable, VendorVariable, vendor);
        }

        private static void Despawn(uint placeable)
        {
            if (!GetIsObjectValid(placeable))
                return;

            var vendor = GetLocalObject(placeable, VendorVariable);
            if (GetIsObjectValid(vendor))
            {
                CityStores.Remove(GetLocalObject(vendor, StoreVariable));
                DestroyObject(vendor);
            }

            DeleteLocalObject(placeable, VendorVariable);
        }
    }
}
