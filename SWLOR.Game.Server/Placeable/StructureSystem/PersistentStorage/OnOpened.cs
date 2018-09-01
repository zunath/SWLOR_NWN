using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.StructureSystem.PersistentStorage
{
    public class OnOpened: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IStructureService _structure;
        private readonly ISerializationService _serialization;

        public OnOpened(INWScript script,
            IStructureService structure,
            ISerializationService serialization)
        {
            _ = script;
            _structure = structure;
            _serialization = serialization;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable chest = NWPlaceable.Wrap(Object.OBJECT_SELF);
            int structureID = chest.GetLocalInt("STRUCTURE_TEMP_STRUCTURE_ID");
            PCTerritoryFlagsStructure entity = _structure.GetPCStructureByID(structureID);
            Location location = chest.Location;

            foreach (PCTerritoryFlagsStructuresItem item in entity.PCTerritoryFlagsStructuresItems)
            {
                _serialization.DeserializeItem(item.ItemObject, chest);
            }

            chest.IsUseable = false;
            return true;
        }
    }
}
