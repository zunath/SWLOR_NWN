using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.ResourceBay
{
    public class OnDisturbed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer player = NWScript.GetLastDisturbed();
            NWPlaceable bay = NWScript.OBJECT_SELF;
            var disturbType = NWScript.GetInventoryDisturbType();
            NWItem item = NWScript.GetInventoryDisturbItem();
            var structureID = bay.GetLocalString("PC_BASE_STRUCTURE_ID");
            var structureGUID = new Guid(structureID);
            var structure = DataService.PCBaseStructure.GetByID(structureGUID);
            var controlTower = BaseService.GetBaseControlTower(structure.PCBaseID);

            if (controlTower == null)
            {
                Console.WriteLine("Could not locate control tower in ResourceBay OnDisturbed. PCBaseID = " + structure.PCBaseID);
                return;
            }

            if (disturbType == DisturbType.Added)
            {
                ItemService.ReturnItem(player, item);
                player.SendMessage("Items cannot be placed inside.");
                return;
            }
            else if (disturbType == DisturbType.Removed)
            {
                var removeItem = DataService.PCBaseStructureItem.GetByPCBaseStructureIDAndItemGlobalIDOrDefault(controlTower.ID, item.GlobalID.ToString());
                if (removeItem == null) return;

                DataService.SubmitDataChange(removeItem, DatabaseActionType.Delete);
            }
        }
    }
}
