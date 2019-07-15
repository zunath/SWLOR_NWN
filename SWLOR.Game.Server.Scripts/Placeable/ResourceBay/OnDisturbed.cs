using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.ResourceBay
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
            NWPlayer player = _.GetLastDisturbed();
            NWPlaceable bay = NWGameObject.OBJECT_SELF;
            int disturbType = _.GetInventoryDisturbType();
            NWItem item = _.GetInventoryDisturbItem();
            string structureID = bay.GetLocalString("PC_BASE_STRUCTURE_ID");
            Guid structureGUID = new Guid(structureID);
            var structure = DataService.PCBaseStructure.GetByID(structureGUID);
            var controlTower = BaseService.GetBaseControlTower(structure.PCBaseID);

            if (disturbType == _.INVENTORY_DISTURB_TYPE_ADDED)
            {
                ItemService.ReturnItem(player, item);
                player.SendMessage("Items cannot be placed inside.");
                return;
            }
            else if (disturbType == _.INVENTORY_DISTURB_TYPE_REMOVED)
            {
                var removeItem = DataService.PCBaseStructureItem.GetByPCBaseStructureIDAndItemGlobalIDOrDefault(controlTower.ID, item.GlobalID.ToString());
                if (removeItem == null) return;

                DataService.SubmitDataChange(removeItem, DatabaseActionType.Delete);
            }
        }
    }
}
