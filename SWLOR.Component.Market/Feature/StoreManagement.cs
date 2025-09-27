using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Market.Feature
{
    public class StoreManagement
    {
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;

        public StoreManagement(ILogger logger, IScheduler scheduler)
        {
            _logger = logger;
            _scheduler = scheduler;
        }

        private const int IntervalHours = 1; // Determines the interval at which stores are cleaned. 1 = 1 hour
        private static readonly List<uint> _stores = new();
        private const string StoreServiceItem = "STORE_SERVICE_IS_STORE_ITEM";

        public void ProcessStores()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                for (var store = GetFirstObjectInArea(area); GetIsObjectValid(store); store = GetNextObjectInArea(area))
                {
                    if (GetObjectType(store) != ObjectType.Store)
                        continue;

                    for (var item = GetFirstItemInInventory(store); GetIsObjectValid(item); item = GetNextItemInInventory(store))
                    {
                        SetLocalBool(item, StoreServiceItem, true);
                        ApplyIncreasedPriceItemProperty(item);
                    }

                    _stores.Add(store);
                }
            }

            _scheduler.ScheduleRepeating(DoCleanStores, TimeSpan.FromHours(IntervalHours));
        }

        public void AcquireItem()
        {
            ClearStoreServiceItemFlag();
            HandleIncreasedPriceItemProperty();
        }

        private void ClearStoreServiceItemFlag()
        {
            var item = GetModuleItemAcquired();
            DeleteLocalBool(item, StoreServiceItem);
        }
        
        private void HandleIncreasedPriceItemProperty()
        {
            var item = GetModuleItemAcquired();
            var creature = GetModuleItemAcquiredBy();

            if (GetIsPC(creature))
            {
                BiowareXP2.IPRemoveMatchingItemProperties(item, ItemPropertyType.IncreasedPrice, DurationType.Invalid, -1);
            }
            else
            {
                ApplyIncreasedPriceItemProperty(item);
            }
        }

        private void ApplyIncreasedPriceItemProperty(uint item)
        {
            var increasedPrice = 0;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.IncreasedPrice)
                {
                    var index = GetItemPropertyCostTableValue(ip);
                    var value = Get2DAString("iprp_incprice", "Cost", index);
                    if (int.TryParse(value, out var price))
                    {
                        increasedPrice += price;
                    }
                }
            }

            if (increasedPrice > 0)
            {
                var price = ItemPlugin.GetBaseGoldPieceValue(item) + increasedPrice;
                ItemPlugin.SetBaseGoldPieceValue(item, price);
            }
        }

        /// <summary>
        /// Iterates through all the stores and destroys any non-store items.
        /// </summary>
        private void DoCleanStores()
        {
            foreach (var store in _stores)
            {
                var count = 0;
                for (var item = GetFirstItemInInventory(store); GetIsObjectValid(item); item = GetNextItemInInventory(store))
                {
                    if (GetLocalBool(item, StoreServiceItem))
                        continue;

                    DestroyObject(item);
                    count++;
                }

                _logger.Write<StoreCleanupLogGroup>($"Store cleaned: {GetName(store)}. Items destroyed: {count}");
            }
        }

        public void DestroySoldItem()
        {
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var isSuccessful = EventsPlugin.GetEventData("RESULT") == "1";

            if (!isSuccessful)
                return;

            DestroyObject(item);
        }

        public void PreventSalesFromHenchmenInventory()
        {
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var owner = GetItemPossessor(item);
            var master = GetMaster(owner);

            if (GetIsObjectValid(master))
            {
                EventsPlugin.SkipEvent();
                SendMessageToPC(master, ColorToken.Red("Items cannot be directly sold from your henchman's inventory."));
            }
        }




    }
}
