using System;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
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
            NWPlaceable bay = _.OBJECT_SELF;
            var disturbType = _.GetInventoryDisturbType();
            NWItem item = _.GetInventoryDisturbItem();
            string structureID = bay.GetLocalString("PC_BASE_STRUCTURE_ID");
            Guid structureGUID = new Guid(structureID);
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
