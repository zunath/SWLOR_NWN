using System;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
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
            var pcBaseStructure = _data.Get<PCBaseStructure>(pcBaseStructureItem.PCBaseStructureID);
            var pcBase = _data.Get<PCBase>(pcBaseStructure.PCBaseID);

            var impoundItem = new PCImpoundedItem
            {
                DateImpounded = DateTime.UtcNow,
                ItemName = pcBaseStructureItem.ItemName,
                ItemResref = pcBaseStructureItem.ItemResref,
                ItemObject = pcBaseStructureItem.ItemObject,
                ItemTag = pcBaseStructureItem.ItemTag,
                PlayerID = pcBase.PlayerID
            };
            _data.SubmitDataChange(impoundItem, DatabaseActionType.Insert);
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
            _data.SubmitDataChange(structureImpoundedItem, DatabaseActionType.Insert);
        }
    }
}
