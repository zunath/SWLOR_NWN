using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Placeable.StructureStorage
{
    public class OnDisturbed : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IColorTokenService _color;
        private readonly ISerializationService _serialization;
        private readonly IItemService _item;

        public OnDisturbed(INWScript script,
            IDataService data,
            IColorTokenService color,
            ISerializationService serialization,
            IItemService item)
        {
            _ = script;
            _data = data;
            _color = color;
            _serialization = serialization;
            _item = item;
        }

        public bool Run(params object[] args)
        {
            NWPlayer oPC = (_.GetLastDisturbed());
            NWItem item = (_.GetInventoryDisturbItem());
            NWPlaceable container = (Object.OBJECT_SELF);
            int disturbType = _.GetInventoryDisturbType();
            int structureID = container.GetLocalInt("PC_BASE_STRUCTURE_ID");
            var structure = _data.Single<PCBaseStructure>(x => x.PCBaseStructureID == structureID);
            var baseStructure = _data.Get<BaseStructure>(structure.BaseStructureID);
            int itemLimit = baseStructure.Storage + structure.StructureBonus;

            int itemCount = container.InventoryItems.Count();
            string itemResref = item.Resref;

            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                if (itemCount > itemLimit)
                {
                    ReturnItem(oPC, item);
                    oPC.SendMessage(_color.Red("No more items can be placed inside."));
                }
                else if (item.BaseItemType == BASE_ITEM_GOLD)
                {
                    ReturnItem(oPC, item);
                    oPC.SendMessage(_color.Red("Credits cannot be placed inside."));
                }
                else
                {
                    PCBaseStructureItem itemEntity = new PCBaseStructureItem
                    {
                        ItemName = item.Name,
                        ItemResref = itemResref,
                        ItemTag = item.Tag,
                        PCBaseStructureID = structureID,
                        ItemGlobalID = item.GlobalID,
                        ItemObject = _serialization.Serialize(item)
                    };

                    structure.PCBaseStructureItems.Add(itemEntity);
                }
            }
            else if (disturbType == INVENTORY_DISTURB_TYPE_REMOVED)
            {
                var dbItem = _data.Single<PCBaseStructureItem>(x => x.ItemGlobalID == item.GlobalID);
                _data.SubmitDataChange(dbItem, DatabaseActionType.Delete);
            }

            oPC.SendMessage(_color.White("Item Limit: " + itemCount + " / ") + _color.Red(itemLimit.ToString()));

            return true;
        }

        private void ReturnItem(NWPlayer oPC, NWItem oItem)
        {
            _.CopyItem(oItem.Object, oPC.Object, TRUE);
            oItem.Destroy();
        }

    }
}
