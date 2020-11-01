using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.StructureStorage
{
    public class OnOpened : IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable chest = (NWScript.OBJECT_SELF);
            var structureID = new Guid(chest.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = DataService.PCBaseStructure.GetByID(structureID);

            var items = DataService.PCBaseStructureItem.GetAllByPCBaseStructureID(structure.ID);
            foreach (var item in items)
            {
                SerializationService.DeserializeItem(item.ItemObject, chest);
            }

            chest.IsUseable = false;
        }
    }
}
