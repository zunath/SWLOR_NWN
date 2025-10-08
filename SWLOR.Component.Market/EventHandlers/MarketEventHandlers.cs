using SWLOR.Component.Market.Contracts;
using SWLOR.Component.Market.Feature;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
namespace SWLOR.Component.Market.EventHandlers
{
    /// <summary>
    /// Event handlers for Market-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the appropriate services.
    /// </summary>
    public class MarketEventHandlers : IEventHandler
    {
        private readonly IPlayerMarketService _playerMarketService;
        private readonly StoreManagement _storeManagement;

        public MarketEventHandlers(IPlayerMarketService playerMarketService, StoreManagement storeManagement)
        {
            _playerMarketService = playerMarketService;
            _storeManagement = storeManagement;
        }

        /// <summary>
        /// When the module caches, cache all static player market data for quick retrieval.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            _playerMarketService.LoadMarketCategories();
            _playerMarketService.LoadMarkets();
        }

        /// <summary>
        /// Marks items as unlisted if they have been sitting on the market for longer than two weeks.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void RemoveOldListings()
        {
            _playerMarketService.RemoveOldListings();
        }

        /// <summary>
        /// When a player enters the server, if they have credits in their market till, send them a message stating so.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void CheckMarketTill()
        {
            _playerMarketService.CheckMarketTill();
        }

        /// <summary>
        /// When the module loads, place all stores inside the cache and schedule the cleanup process.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void OnModuleLoad()
        {
            _storeManagement.ProcessStores();
        }

        /// <summary>
        /// When a store item is acquired, destroy the local flag indicating it's a store item.
        /// </summary>
        [ScriptHandler<OnModuleAcquire>]
        public void OnModuleAcquire()
        {
            _storeManagement.AcquireItem();
        }

        /// <summary>
        /// Destroys items sold to NPC stores immediately.
        /// </summary>
        [ScriptHandler<OnStoreSellAfter>]
        public void OnStoreSellAfter()
        {
            _storeManagement.DestroySoldItem();
        }

        /// <summary>
        /// Prevents items from being sold from a henchman's inventory.
        /// </summary>
        [ScriptHandler<OnStoreSellBefore>]
        public void OnStoreSellBefore()
        {
            _storeManagement.PreventSalesFromHenchmenInventory();
        }
    }
}


