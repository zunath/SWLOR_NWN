using SWLOR.Component.Market.Contracts;
using SWLOR.Component.Market.Feature;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Server;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Market.EventHandlers
{
    /// <summary>
    /// Event handlers for Market-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the appropriate services.
    /// </summary>
    public class MarketEventHandlers
    {
        private readonly IPlayerMarketService _playerMarketService;
        private readonly StoreManagement _storeManagement;

        public MarketEventHandlers(
            IPlayerMarketService playerMarketService,
            StoreManagement storeManagement,
            IEventAggregator eventAggregator)
        {
            _playerMarketService = playerMarketService;
            _storeManagement = storeManagement;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheData());
            eventAggregator.Subscribe<OnServerHeartbeat>(e => RemoveOldListings());
            eventAggregator.Subscribe<OnModuleEnter>(e => CheckMarketTill());
            eventAggregator.Subscribe<OnModuleLoad>(e => OnModuleLoad());
            eventAggregator.Subscribe<OnModuleAcquire>(e => OnModuleAcquire());
            eventAggregator.Subscribe<OnStoreSellAfter>(e => OnStoreSellAfter());
            eventAggregator.Subscribe<OnStoreSellBefore>(e => OnStoreSellBefore());
        }

        /// <summary>
        /// When the module caches, cache all static player market data for quick retrieval.
        /// </summary>
        public void CacheData()
        {
            _playerMarketService.LoadMarketCategories();
            _playerMarketService.LoadMarkets();
        }

        /// <summary>
        /// Marks items as unlisted if they have been sitting on the market for longer than two weeks.
        /// </summary>
        public void RemoveOldListings()
        {
            _playerMarketService.RemoveOldListings();
        }

        /// <summary>
        /// When a player enters the server, if they have credits in their market till, send them a message stating so.
        /// </summary>
        public void CheckMarketTill()
        {
            _playerMarketService.CheckMarketTill();
        }

        /// <summary>
        /// When the module loads, place all stores inside the cache and schedule the cleanup process.
        /// </summary>
        public void OnModuleLoad()
        {
            _storeManagement.ProcessStores();
        }

        /// <summary>
        /// When a store item is acquired, destroy the local flag indicating it's a store item.
        /// </summary>
        public void OnModuleAcquire()
        {
            _storeManagement.AcquireItem();
        }

        /// <summary>
        /// Destroys items sold to NPC stores immediately.
        /// </summary>
        public void OnStoreSellAfter()
        {
            _storeManagement.DestroySoldItem();
        }

        /// <summary>
        /// Prevents items from being sold from a henchman's inventory.
        /// </summary>
        public void OnStoreSellBefore()
        {
            _storeManagement.PreventSalesFromHenchmenInventory();
        }
    }
}
