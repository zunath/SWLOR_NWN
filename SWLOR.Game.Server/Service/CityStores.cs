using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The stores of a city's buildings and market stalls (Player City Revamp, "Stores and sales tax"). A store is created
    /// from its blueprint when its building or stall spawns, opens with the city's Sales tax added to the markup, credits
    /// the tax to the treasury when something is bought, and never buys from players. Limited stock is rebuilt from the
    /// saved weekly allowances (<see cref="CityStockAllowances"/>) whenever a store is created or opened. The rules live
    /// in <see cref="CityStoreRules"/>, <see cref="SalesTaxRules"/> and <see cref="StockAllowanceRules"/>.
    /// </summary>
    public static class CityStores
    {
        /// <summary>The tag every city store carries, so staff and vendors find the nearest one.</summary>
        public const string StoreTag = "CITY_STORE";

        private const string CityIdVariable = "CITY_STORE_CITY_ID";
        private const string KindVariable = "CITY_STORE_KIND";
        private const string LevelVariable = "CITY_STORE_LEVEL";
        private const string BlueprintVariable = "CITY_STORE_BLUEPRINT";

        // On the shopper: the rate a store was opened with, the city it belongs to, and the store itself.
        private const string TaxPercentVariable = "CITY_STORE_TAX_PERCENT";
        private const string TaxCityVariable = "CITY_STORE_TAX_CITY";
        private const string OpenStoreVariable = "CITY_STORE_OPEN";

        // On the shopper: the purchase the buy event is about to make.
        private const string PendingResrefVariable = "CITY_STORE_PENDING_RESREF";
        private const string PendingQuantityVariable = "CITY_STORE_PENDING_QUANTITY";
        private const string PendingPriceVariable = "CITY_STORE_PENDING_PRICE";

        // Creating and finding stores

        /// <summary>Whether this object is a store a city created.</summary>
        public static bool IsCityStore(uint store)
        {
            return GetIsObjectValid(store) &&
                   GetObjectType(store) == ObjectType.Store &&
                   !string.IsNullOrWhiteSpace(GetLocalString(store, CityIdVariable));
        }

        /// <summary>The city store nearest to an object (a staff member or a vendor), or an invalid object.</summary>
        public static uint FindNearest(uint near)
        {
            return GetNearestObjectByTag(StoreTag, near);
        }

        /// <summary>The level a city's store of this kind is at: its building's level, the city's for City Hall, and 1 for a stall.</summary>
        public static int GetLevel(WorldProperty city, CityStoreKind kind)
        {
            var level = kind switch
            {
                CityStoreKind.CityHall => city.Upgrades.GetValueOrDefault(PropertyUpgradeType.CityLevel, 1),
                CityStoreKind.Cantina => Property.GetEffectiveUpgradeLevel(city.Id, PropertyUpgradeType.CantinaLevel),
                CityStoreKind.MedicalCenter => Property.GetEffectiveUpgradeLevel(city.Id, PropertyUpgradeType.MedicalCenterLevel),
                CityStoreKind.Starport => Property.GetEffectiveUpgradeLevel(city.Id, PropertyUpgradeType.StarportLevel),
                _ => 1
            };

            return System.Math.Max(1, level);
        }

        /// <summary>
        /// Creates a city's store of this kind at a location, from the blueprint its level opens, with its limited stock on
        /// the shelf. Returns an invalid object when the store cannot be made.
        /// </summary>
        public static uint Create(WorldProperty city, CityStoreKind kind, Location location)
        {
            var detail = CityStoreCatalog.Get(kind);
            if (city == null || detail == null)
                return OBJECT_INVALID;

            var level = GetLevel(city, kind);
            var blueprint = CityStoreRules.GetBlueprint(detail, level);
            var store = CreateObject(ObjectType.Store, blueprint, location, false, StoreTag);
            if (!GetIsObjectValid(store))
            {
                Log.Write(LogGroup.Error, $"City store '{blueprint}' ({kind}) of city '{city.CustomName}' ({city.Id}) could not be created. Check that the blueprint is in the module.", true);
                return OBJECT_INVALID;
            }

            SetName(store, detail.Name);
            SetLocalString(store, CityIdVariable, city.Id);
            SetLocalInt(store, KindVariable, (int)kind);
            SetLocalInt(store, LevelVariable, level);
            SetLocalString(store, BlueprintVariable, blueprint);
            StockLimitedItems(store, city.Id, detail, level);

            return store;
        }

        /// <summary>Removes a store and anything that is left of its stock.</summary>
        public static void Remove(uint store)
        {
            if (GetIsObjectValid(store))
                DestroyObject(store);
        }

        /// <summary>
        /// Brings an open-able store up to date: a store built before its facility was upgraded, or before a stall's
        /// theme changed, is replaced by one from the blueprint the city now opens, and the limited stock is rebuilt from
        /// the allowances. Returns the store to open.
        /// </summary>
        private static uint Refresh(uint store, WorldProperty city)
        {
            var kind = (CityStoreKind)GetLocalInt(store, KindVariable);
            var detail = CityStoreCatalog.Get(kind);
            if (detail == null)
                return store;

            var level = GetLevel(city, kind);
            var blueprint = CityStoreRules.GetBlueprint(detail, level);
            if (GetLocalString(store, BlueprintVariable) != blueprint)
            {
                var replacement = Create(city, kind, GetLocation(store));
                if (GetIsObjectValid(replacement))
                {
                    Remove(store);
                    return replacement;
                }

                return store;
            }

            SetLocalInt(store, LevelVariable, level);
            StockLimitedItems(store, city.Id, detail, level);
            return store;
        }

        /// <summary>Puts the limited items on the shelf for what is left of their weekly allowances.</summary>
        private static void StockLimitedItems(uint store, string cityId, CityStoreDetail detail, int level)
        {
            if (detail.LimitedItems.Count == 0)
                return;

            var limited = new HashSet<string>(CityStoreRules.GetLimitedResrefs(detail));
            var stale = new List<uint>();
            for (var item = GetFirstItemInInventory(store); GetIsObjectValid(item); item = GetNextItemInInventory(store))
            {
                if (limited.Contains(GetResRef(item)))
                    stale.Add(item);
            }

            foreach (var item in stale)
                DestroyObject(item);

            foreach (var entry in CityStockAllowances.PlanShelf(cityId, detail, level, DateTime.UtcNow))
            {
                for (var i = 0; i < entry.Quantity; i++)
                    CreateItemOnObject(entry.Resref, store);
            }
        }

        // Opening

        /// <summary>The city a store belongs to, or null when it has none or the city is gone.</summary>
        private static WorldProperty GetCity(uint store)
        {
            var cityId = GetLocalString(store, CityIdVariable);
            if (string.IsNullOrWhiteSpace(cityId))
                return null;

            var city = DB.Get<WorldProperty>(cityId);
            return city == null || city.IsQueuedForDeletion ? null : city;
        }

        /// <summary>The rate a city's stores add to every price. Cities that never set one charge nothing.</summary>
        public static int GetSalesTax(WorldProperty city)
        {
            return city != null && city.Taxes.TryGetValue(PropertyTaxType.Sales, out var percent)
                ? SalesTaxRules.ClampPercent(percent)
                : 0;
        }

        /// <summary>
        /// Opens a city store for a player with the city's Sales tax added to the markup. A citizen of a city this city has
        /// an Embargo against is refused. The charged rate and city are kept on the player, because the mayor can change
        /// the rate while the store is open and the sale is credited at the rate it was made at.
        /// </summary>
        /// <returns>False when the store could not be opened.</returns>
        public static bool Open(uint player, uint store)
        {
            if (!IsCityStore(store))
            {
                if (GetIsObjectValid(store))
                    NWScript.OpenStore(store, player);

                return GetIsObjectValid(store);
            }

            var city = GetCity(store);
            if (city == null)
            {
                SendMessageToPC(player, "This store is closed.");
                return false;
            }

            var dbPlayer = GetIsPC(player) ? DB.Get<Player>(GetObjectUUID(player)) : null;
            var embargo = EdictChanges.GetInEffect(city, EdictType.Embargo, DateTime.UtcNow);
            if (!CityStoreRules.CanShop(embargo?.TargetCityId, dbPlayer?.CitizenPropertyId))
            {
                SendMessageToPC(player, ColorToken.Red($"{city.CustomName} has an embargo against your city. Its stores will not serve you."));
                return false;
            }

            store = Refresh(store, city);
            if (!GetIsObjectValid(store))
                return false;

            var percent = GetSalesTax(city);
            SetLocalString(player, TaxCityVariable, city.Id);
            SetLocalInt(player, TaxPercentVariable, percent);
            SetLocalObject(player, OpenStoreVariable, store);

            if (percent > 0)
                SendMessageToPC(player, SalesTaxRules.Describe(percent));

            NWScript.OpenStore(store, player, percent);
            return true;
        }

        /// <summary>Opens the city store nearest to a staff member or vendor for a player.</summary>
        public static bool OpenNearest(uint player, uint staff)
        {
            var store = FindNearest(staff);
            if (!GetIsObjectValid(store))
            {
                SendMessageToPC(player, "This store is not stocked yet.");
                return false;
            }

            return Open(player, store);
        }

        // Buying and selling

        /// <summary>
        /// Before a purchase from a city store: refuses a limited item that has no allowance left this week, and keeps
        /// what is being bought, since the item may be merged into the buyer's stack by the time the purchase is over.
        /// </summary>
        [NWNEventHandler(ScriptName.OnStoreRequestBuyBefore)]
        public static void OnBuyBefore()
        {
            var store = StringToObject(EventsPlugin.GetEventData("STORE"));
            if (!IsCityStore(store))
                return;

            var buyer = OBJECT_SELF;
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            if (!GetIsObjectValid(item))
                return;

            var resref = GetResRef(item);
            var quantity = System.Math.Max(1, GetItemStackSize(item));
            var price = int.TryParse(EventsPlugin.GetEventData("PRICE"), out var parsedPrice) ? parsedPrice : 0;

            var city = GetCity(store);
            var kind = (CityStoreKind)GetLocalInt(store, KindVariable);
            var limited = CityStoreRules.FindLimitedItem(CityStoreCatalog.Get(kind), GetLocalInt(store, LevelVariable), resref);
            if (city != null && limited != null &&
                CityStockAllowances.GetRemaining(city.Id, kind, limited, DateTime.UtcNow) < quantity)
            {
                EventsPlugin.SkipEvent();
                SendMessageToPC(buyer, ColorToken.Red("That item is sold out here until the weekly restock."));
                return;
            }

            SetLocalString(buyer, PendingResrefVariable, resref);
            SetLocalInt(buyer, PendingQuantityVariable, quantity);
            SetLocalInt(buyer, PendingPriceVariable, price);
        }

        /// <summary>
        /// After a purchase from a city store: credits the tax part of the price to the city's treasury, rounded down, and
        /// counts a limited item against its weekly allowance.
        /// </summary>
        [NWNEventHandler(ScriptName.OnStoreRequestBuyAfter)]
        public static void OnBuyAfter()
        {
            var store = StringToObject(EventsPlugin.GetEventData("STORE"));
            var buyer = OBJECT_SELF;
            var resref = GetLocalString(buyer, PendingResrefVariable);
            var quantity = GetLocalInt(buyer, PendingQuantityVariable);
            var pendingPrice = GetLocalInt(buyer, PendingPriceVariable);
            DeleteLocalString(buyer, PendingResrefVariable);
            DeleteLocalInt(buyer, PendingQuantityVariable);
            DeleteLocalInt(buyer, PendingPriceVariable);

            if (!IsCityStore(store) || EventsPlugin.GetEventData("RESULT") != "1")
                return;

            var cityId = GetLocalString(store, CityIdVariable);
            var kind = (CityStoreKind)GetLocalInt(store, KindVariable);
            var price = int.TryParse(EventsPlugin.GetEventData("PRICE"), out var parsedPrice) ? parsedPrice : pendingPrice;
            var percent = GetLocalString(buyer, TaxCityVariable) == cityId ? GetLocalInt(buyer, TaxPercentVariable) : 0;

            var tax = SalesTaxRules.GetTaxPortion(price, percent);
            if (tax > 0)
                CreditTax(cityId, tax, buyer);

            var limited = CityStoreRules.FindLimitedItem(CityStoreCatalog.Get(kind), GetLocalInt(store, LevelVariable), resref);
            if (limited != null && quantity > 0)
                CityStockAllowances.RecordSale(cityId, kind, resref, quantity, DateTime.UtcNow);
        }

        private static void CreditTax(string cityId, int tax, uint buyer)
        {
            var city = DB.Get<WorldProperty>(cityId);
            if (city == null)
                return;

            city.Treasury += tax;
            DB.Set(city);
            Log.Write(LogGroup.Property, $"SALES TAX - {PlayerName.GetAuditName(buyer)} paid {tax} credits to the treasury of {city.CustomName} ({city.Id}).");
        }

        /// <summary>City stores only sell: a sale to one is refused, so there is no buyback and no buy-sell loop.</summary>
        [NWNEventHandler(ScriptName.OnStoreRequestSellBefore)]
        public static void OnSellBefore()
        {
            var seller = OBJECT_SELF;
            var store = StringToObject(EventsPlugin.GetEventData("STORE"));
            if (!GetIsObjectValid(store))
                store = GetLocalObject(seller, OpenStoreVariable);

            if (!IsCityStore(store))
                return;

            EventsPlugin.SkipEvent();
            SendMessageToPC(seller, ColorToken.Red("City stores only sell. They do not buy from players."));
        }
    }
}
