using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Combat.Events;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Events;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Inventory.EventHandlers
{
    public class InventoryServiceEventHandlers
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<ILootService> _lootService;
        private readonly Lazy<IKeyItemService> _keyItemService;

        public InventoryServiceEventHandlers(
            IServiceProvider serviceProvider,
            IEventAggregator eventAggregator)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _lootService = new Lazy<ILootService>(() => _serviceProvider.GetRequiredService<ILootService>());
            _keyItemService = new Lazy<IKeyItemService>(() => _serviceProvider.GetRequiredService<IKeyItemService>());

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheItemData());
            eventAggregator.Subscribe<OnValidateUseItemAfter>(e => UseItem());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => RegisterLootTables());
            eventAggregator.Subscribe<OnDealtDamage>(e => SpawnStealLoot());
            eventAggregator.Subscribe<OnCorpseClosed>(e => CloseCorpseContainer());
            eventAggregator.Subscribe<OnCorpseDisturbed>(e => DisturbCorpseContainer());
            eventAggregator.Subscribe<OnCreatureDeathAfter>(e => SpawnLootOnCreatureDeath());
            eventAggregator.Subscribe<OnPlayerTargetUpdated>(e => MarkCreditfinderAndTreasureHunterOnTarget());
            eventAggregator.Subscribe<OnObjectDestroyed>(e => ProcessCorpse());
            eventAggregator.Subscribe<OnGetKeyItem>(e => ObtainKeyItem());
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _itemService.Value;
        private ILootService LootService => _lootService.Value;
        private IKeyItemService KeyItemService => _keyItemService.Value;
        public void CacheItemData()
        {
            ItemService.CacheData();
            KeyItemService.CacheData();
        }
        public void UseItem()
        {
            ItemService.UseItem();
        }
        public void RegisterLootTables()
        {
            LootService.RegisterLootTables();
        }
        public void SpawnStealLoot()
        {
            LootService.SpawnStealLoot();
        }
        public void CloseCorpseContainer()
        {
            LootService.CloseCorpseContainer();
        }
        public void DisturbCorpseContainer()
        {
            LootService.DisturbCorpseContainer();
        }
        public void SpawnLootOnCreatureDeath()
        {
            LootService.SpawnLootOnCreatureDeath();
        }
        public void MarkCreditfinderAndTreasureHunterOnTarget()
        {
            LootService.MarkCreditfinderAndTreasureHunterOnTarget();
        }
        public void ProcessCorpse()
        {
            LootService.ProcessCorpse();
        }
        public void ObtainKeyItem()
        {
            KeyItemService.ObtainKeyItem();
        }
    }
}
