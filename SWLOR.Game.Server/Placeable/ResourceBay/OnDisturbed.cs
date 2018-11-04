using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.ResourceBay
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IItemService _item;
        private readonly IBaseService _base;

        public OnDisturbed(
            INWScript script,
            IDataService data,
            IItemService item,
            IBaseService @base)
        {
            _ = script;
            _data = data;
            _item = item;
            _base = @base;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = _.GetLastDisturbed();
            NWPlaceable bay = Object.OBJECT_SELF;
            int disturbType = _.GetInventoryDisturbType();
            NWItem item = _.GetInventoryDisturbItem();
            int structureID = bay.GetLocalInt("PC_BASE_STRUCTURE_ID");
            var structure = _data.Single<PCBaseStructure>(x => x.PCBaseStructureID == structureID);
            var controlTower = _base.GetBaseControlTower(structure.PCBaseID);

            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                _item.ReturnItem(player, item);
                player.SendMessage("Items cannot be placed inside.");
                return false;
            }
            else if (disturbType == INVENTORY_DISTURB_TYPE_REMOVED)
            {
                var removeItem = _data.SingleOrDefault<PCBaseStructureItem>(x => x.PCBaseStructureID == controlTower.PCBaseStructureID && x.ItemGlobalID == item.GlobalID);
                if (removeItem == null) return false;

                _data.SubmitDataChange(removeItem, DatabaseActionType.Delete);
            }

            return true;
        }
    }
}
