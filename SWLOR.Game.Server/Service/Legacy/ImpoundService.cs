using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;


namespace SWLOR.Game.Server.Service
{
    public static class ImpoundService
    {
        public static void Impound(PCBaseStructureItem pcBaseStructureItem)
        {
            var pcBaseStructure = DataService.PCBaseStructure.GetByID(pcBaseStructureItem.PCBaseStructureID);
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
            DataService.SubmitDataChange(impoundItem, DatabaseActionType.Insert);
        }

        public static void Impound(Guid playerID, NWItem item)
        {
            PCImpoundedItem structureImpoundedItem;
            // Processing a container, impound it's contents first.
            if (item.HasInventory)
            {
                foreach (NWItem inventoryItem in item.InventoryItems)
                {
                    structureImpoundedItem = new PCImpoundedItem
                    {
                        DateImpounded = DateTime.UtcNow,
                        PlayerID = playerID,
                        ItemObject = SerializationService.Serialize(inventoryItem),
                        ItemTag = inventoryItem.Tag,
                        ItemResref = inventoryItem.Resref,
                        ItemName = inventoryItem.Name
                    };
                    DataService.SubmitDataChange(structureImpoundedItem, DatabaseActionType.Insert);
                }
            }

            // Impound parameter item.
            structureImpoundedItem = new PCImpoundedItem
            {
                DateImpounded = DateTime.UtcNow,
                PlayerID = playerID,
                ItemObject = SerializationService.Serialize(item),
                ItemTag = item.Tag,
                ItemResref = item.Resref,
                ItemName = item.Name
            };
            DataService.SubmitDataChange(structureImpoundedItem, DatabaseActionType.Insert);            
        }
    }
}
