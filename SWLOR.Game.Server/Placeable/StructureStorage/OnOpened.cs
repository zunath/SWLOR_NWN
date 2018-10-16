using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.StructureStorage
{
    public class OnOpened : IRegisteredEvent
    {
        private readonly ISerializationService _serialization;
        private readonly IDataContext _db;

        public OnOpened(
            ISerializationService serialization,
            IDataContext db)
        {
            _serialization = serialization;
            _db = db;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable chest = (Object.OBJECT_SELF);
            int structureID = chest.GetLocalInt("PC_BASE_STRUCTURE_ID");
            var structure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == structureID);

            foreach (var item in structure.PCBaseStructureItems)
            {
                _serialization.DeserializeItem(item.ItemObject, chest);
            }

            chest.IsUseable = false;
            return true;
        }
    }
}
