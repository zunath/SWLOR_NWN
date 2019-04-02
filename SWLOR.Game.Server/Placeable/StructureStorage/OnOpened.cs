using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.StructureStorage
{
    public class OnOpened : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable chest = (Object.OBJECT_SELF);
            Guid structureID = new Guid(chest.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = DataService.Single<PCBaseStructure>(x => x.ID == structureID);

            var items = DataService.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == structure.ID);
            foreach (var item in items)
            {
                SerializationService.DeserializeItem(item.ItemObject, chest);
            }

            chest.IsUseable = false;
            return true;
        }
    }
}
