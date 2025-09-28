using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Combat;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Inventory;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Component.Inventory.EventHandlers
{
    public class InventoryServiceEventHandlers
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<ILootService> _lootService;

        public InventoryServiceEventHandlers(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _lootService = new Lazy<ILootService>(() => _serviceProvider.GetRequiredService<ILootService>());
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _itemService.Value;
        private ILootService LootService => _lootService.Value;

        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheItemData()
        {
            ItemService.CacheData();
        }

        [ScriptHandler<OnItemUseBefore>]
        public void UseItem()
        {
            ItemService.UseItem();
        }

        [ScriptHandler<OnModuleCacheBefore>]
        public void RegisterLootTables()
        {
            LootService.RegisterLootTables();
        }

        [ScriptHandler<OnCreatureSpawnBefore>]
        public void SpawnStealLoot()
        {
            LootService.SpawnStealLoot();
        }

        [ScriptHandler<OnCorpseClosed>]
        public void CloseCorpseContainer()
        {
            LootService.CloseCorpseContainer();
        }

        [ScriptHandler<OnCorpseDisturbed>]
        public void DisturbCorpseContainer()
        {
            LootService.DisturbCorpseContainer();
        }

        [ScriptHandler<OnCreatureDeathBefore>]
        public void SpawnLootOnCreatureDeath()
        {
            LootService.SpawnLootOnCreatureDeath();
        }

        [ScriptHandler<OnItemHit>]
        public void MarkCreditfinderAndTreasureHunterOnTarget()
        {
            LootService.MarkCreditfinderAndTreasureHunterOnTarget();
        }

        [ScriptHandler<OnCreatureDeathBefore>]
        public void ProcessCorpse()
        {
            LootService.ProcessCorpse();
        }
    }
}
