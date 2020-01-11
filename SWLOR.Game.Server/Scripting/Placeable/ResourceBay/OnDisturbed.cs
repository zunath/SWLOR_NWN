using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Placeable.ResourceBay
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

            if (disturbType == InventoryDisturbType.Added)
            {
                ItemService.ReturnItem(player, item);
                player.SendMessage("Items cannot be placed inside.");
                return;
            }
            else if (disturbType == InventoryDisturbType.Removed)
            {
                structure.Items.Remove(item.GlobalID);
                DataService.SubmitDataChange(structure, DatabaseActionType.Set);
            }
        }
    }
}
