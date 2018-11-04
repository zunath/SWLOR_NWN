using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.StructureStorage
{
    public class OnOpened : IRegisteredEvent
    {
        private readonly ISerializationService _serialization;
        private readonly IDataService _data;

        public OnOpened(
            ISerializationService serialization,
            IDataService data)
        {
            _serialization = serialization;
            _data = data;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable chest = (Object.OBJECT_SELF);
            int structureID = chest.GetLocalInt("PC_BASE_STRUCTURE_ID");
            var structure = _data.Single<PCBaseStructure>(x => x.PCBaseStructureID == structureID);

            var items = _data.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == structure.PCBaseStructureID);
            foreach (var item in items)
            {
                _serialization.DeserializeItem(item.ItemObject, chest);
            }

            chest.IsUseable = false;
            return true;
        }
    }
}
