using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.StructureSystem.PersistentStorage
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IStructureService _structure;
        private readonly IColorTokenService _color;
        private readonly ISerializationService _serialization;

        public OnDisturbed(INWScript script,
            IStructureService structure,
            IColorTokenService color,
            ISerializationService serialization)
        {
            _ = script;
            _structure = structure;
            _color = color;
            _serialization = serialization;
        }

        public bool Run(params object[] args)
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastDisturbed());
            NWItem item = NWItem.Wrap(_.GetInventoryDisturbItem());
            NWPlaceable container = NWPlaceable.Wrap(Object.OBJECT_SELF);
            int disturbType = _.GetInventoryDisturbType();
            
            int structureID = container.GetLocalInt("STRUCTURE_TEMP_STRUCTURE_ID");
            PCTerritoryFlagsStructure entity = _structure.GetPCStructureByID(structureID);
            int itemCount = container.InventoryItems.Count;
            string itemResref = item.Resref;

            if (disturbType == NWScript.INVENTORY_DISTURB_TYPE_ADDED)
            {
                if (itemCount > entity.StructureBlueprint.ItemStorageCount)
                {
                    ReturnItem(oPC, item);
                    oPC.SendMessage(_color.Red("No more items can be placed inside."));
                }
                // Only specific types of items can be stored in resource bundles
                else if (!string.IsNullOrWhiteSpace(entity.StructureBlueprint.ResourceResref) && itemResref != entity.StructureBlueprint.ResourceResref)
                {
                    ReturnItem(oPC, item);
                    oPC.SendMessage(_color.Red("That item cannot be stored here."));
                }
                else
                {
                    PCTerritoryFlagsStructuresItem itemEntity = new PCTerritoryFlagsStructuresItem
                    {
                        ItemName = item.Name,
                        ItemResref = itemResref,
                        ItemTag = item.Tag,
                        PCStructureID = entity.PCTerritoryFlagStructureID,
                        GlobalID = item.GlobalID,
                        ItemObject = _serialization.Serialize(item)
                    };

                    entity.PCTerritoryFlagsStructuresItems.Add(itemEntity);
                    _structure.SaveChanges();
                }
            }
            else if (disturbType == NWScript.INVENTORY_DISTURB_TYPE_REMOVED)
            {    
                _structure.DeleteContainerItemByGlobalID(item.GlobalID);
            }

            oPC.SendMessage(_color.White("Item Limit: " + itemCount + " / ") + _color.Red(entity.StructureBlueprint.ItemStorageCount.ToString()));

            return true;
        }

        private void ReturnItem(NWPlayer oPC, NWItem oItem)
        {
            _.CopyItem(oItem.Object, oPC.Object, NWScript.TRUE);
            oItem.Destroy();
        }
        
    }
}
