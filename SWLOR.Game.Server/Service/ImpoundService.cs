using System;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class ImpoundService : IImpoundService
    {
        private readonly IDataService _data;
        private readonly ISerializationService _serialization;

        public ImpoundService(
            IDataService data,
            ISerializationService serialization)
        {
            _data = data;
            _serialization = serialization;
        }

        public void Impound(PCBaseStructureItem pcBaseStructureItem)
        {
            var impoundItem = new PCImpoundedItem
            {
                DateImpounded = DateTime.UtcNow,
                ItemName = pcBaseStructureItem.ItemName,
                ItemResref = pcBaseStructureItem.ItemResref,
                ItemObject = pcBaseStructureItem.ItemObject,
                ItemTag = pcBaseStructureItem.ItemTag,
                PlayerID = pcBaseStructureItem.PCBaseStructure.PCBase.PlayerID
            };

            _data.PCImpoundedItems.Add(impoundItem);
            _data.SaveChanges();
        }

        public void Impound(string playerID, NWItem item)
        {

            PCImpoundedItem structureImpoundedItem = new PCImpoundedItem
            {
                DateImpounded = DateTime.UtcNow,
                PlayerID = playerID,
                ItemObject = _serialization.Serialize(item),
                ItemTag = item.Tag,
                ItemResref = item.Resref,
                ItemName = item.Name
            };

            _data.PCImpoundedItems.Add(structureImpoundedItem);
            _data.SaveChanges();
        }
    }
}
