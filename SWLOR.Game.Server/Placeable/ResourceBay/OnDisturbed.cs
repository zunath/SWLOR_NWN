using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.ResourceBay
{
    public class OnDisturbed: IRegisteredEvent
    {
        
        
        
        private readonly IBaseService _base;

        public OnDisturbed(
            
            
            
            IBaseService @base)
        {
            
            
            
            _base = @base;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = _.GetLastDisturbed();
            NWPlaceable bay = Object.OBJECT_SELF;
            int disturbType = _.GetInventoryDisturbType();
            NWItem item = _.GetInventoryDisturbItem();
            string structureID = bay.GetLocalString("PC_BASE_STRUCTURE_ID");
            Guid structureGUID = new Guid(structureID);
            var structure = DataService.Single<PCBaseStructure>(x => x.ID == structureGUID);
            var controlTower = _base.GetBaseControlTower(structure.PCBaseID);

            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                ItemService.ReturnItem(player, item);
                player.SendMessage("Items cannot be placed inside.");
                return false;
            }
            else if (disturbType == INVENTORY_DISTURB_TYPE_REMOVED)
            {
                var removeItem = DataService.SingleOrDefault<PCBaseStructureItem>(x => x.PCBaseStructureID == controlTower.ID && x.ItemGlobalID == item.GlobalID.ToString());
                if (removeItem == null) return false;

                DataService.SubmitDataChange(removeItem, DatabaseActionType.Delete);
            }

            return true;
        }
    }
}
