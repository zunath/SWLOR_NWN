using System;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;


namespace SWLOR.Game.Server.Service
{
    public static class ImpoundService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => PruneOldImpoundItems());
        }

        private static void PruneOldImpoundItems()
        {
            var appSettings = ApplicationSettings.Get();

            if (appSettings.ImpoundPruneDays <= 0)
            {
                Console.WriteLine("Impound item pruning is disabled. Set the SWLOR_IMPOUND_PRUNE_DAYS environment variable if you wish to enable this.");
                return;
            }

            var now = DateTime.UtcNow;
            var impoundedItems = DataService.PCImpoundedItem.GetAll()
                .Where(x => (now - x.DateImpounded).TotalDays >= appSettings.ImpoundPruneDays)
                .ToList();

            Console.WriteLine($"{impoundedItems.Count} impounded items are older than {appSettings.ImpoundPruneDays} days. Pruning them now.");
            foreach (var item in impoundedItems)
            {
                DataService.SubmitDataChange(item, DatabaseActionType.Delete);
            }
            Console.WriteLine($"{impoundedItems.Count} impounded items have been pruned.");
        }

        public static void Impound(Guid pcBaseStructureID, PCBaseStructureItem pcBaseStructureItem)
        {
            var pcBaseStructure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
            var pcBase = DataService.PCBase.GetByID(pcBaseStructure.PCBaseID);

            var impoundItem = new PCImpoundedItem
            {
                DateImpounded = DateTime.UtcNow,
                ItemName = pcBaseStructureItem.ItemName,
                ItemResref = pcBaseStructureItem.ItemResref,
                ItemObject = pcBaseStructureItem.ItemObject,
                ItemTag = pcBaseStructureItem.ItemTag,
                PlayerID = pcBase.PlayerID
            };
            DataService.SubmitDataChange(impoundItem, DatabaseActionType.Set);
        }

        public static void Impound(Guid playerID, NWItem item)
        {
            PCImpoundedItem structureImpoundedItem = new PCImpoundedItem
            {
                DateImpounded = DateTime.UtcNow,
                PlayerID = playerID,
                ItemObject = SerializationService.Serialize(item),
                ItemTag = item.Tag,
                ItemResref = item.Resref,
                ItemName = item.Name
            };
            DataService.SubmitDataChange(structureImpoundedItem, DatabaseActionType.Set);
        }
    }
}
