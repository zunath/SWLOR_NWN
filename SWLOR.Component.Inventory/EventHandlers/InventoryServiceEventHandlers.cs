using SWLOR.Component.Inventory.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Droids.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Component.Inventory.EventHandlers
{
    public class InventoryServiceEventHandlers
    {
        private readonly IItemService _itemService;
        private readonly ILootService _lootService;

        public InventoryServiceEventHandlers(IItemService itemService, ILootService lootService)
        {
            _itemService = itemService;
            _lootService = lootService;
        }

        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheItemData()
        {
            _itemService.CacheData();
        }

        [ScriptHandler<OnItemUseBefore>]
        public void UseItem()
        {
            _itemService.UseItem();
        }

        [ScriptHandler<OnModuleCacheBefore>]
        public void RegisterLootTables()
        {
            _lootService.RegisterLootTables();
        }

        [ScriptHandler<OnCreatureSpawnBefore>]
        public void SpawnStealLoot()
        {
            _lootService.SpawnStealLoot();
        }

        [ScriptHandler(ScriptName.OnCorpseClosed)]
        public void CloseCorpseContainer()
        {
            _lootService.CloseCorpseContainer();
        }

        [ScriptHandler(ScriptName.OnCorpseDisturbed)]
        public void DisturbCorpseContainer()
        {
            _lootService.DisturbCorpseContainer();
        }

        [ScriptHandler<OnCreatureDeathBefore>]
        public void SpawnLootOnCreatureDeath()
        {
            _lootService.SpawnLootOnCreatureDeath();
        }

        [ScriptHandler(ScriptName.OnItemHit)]
        public void MarkCreditfinderAndTreasureHunterOnTarget()
        {
            _lootService.MarkCreditfinderAndTreasureHunterOnTarget();
        }

        [ScriptHandler<OnCreatureDeathBefore>]
        public void ProcessCorpse()
        {
            _lootService.ProcessCorpse();
        }
    }
}
