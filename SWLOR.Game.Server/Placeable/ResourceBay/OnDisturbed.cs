using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Placeable.ResourceBay
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IItemService _item;
        private readonly IBaseService _base;

        public OnDisturbed(
            INWScript script,
            IDataContext db,
            IItemService item,
            IBaseService @base)
        {
            _ = script;
            _db = db;
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
            var controlTower = _base.GetBaseControlTower(structureID);

            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                _item.ReturnItem(player, item);
                player.SendMessage("Items cannot be placed inside.");
                return false;
            }
            else if (disturbType == INVENTORY_DISTURB_TYPE_REMOVED)
            {
                var removeItem = controlTower.PCBaseStructureItems.SingleOrDefault(x => x.ItemGlobalID == item.GlobalID);
                if (removeItem == null) return false;

                controlTower.PCBaseStructureItems.Remove(removeItem);

                _db.SaveChanges();
            }

            return true;
        }
    }
}
