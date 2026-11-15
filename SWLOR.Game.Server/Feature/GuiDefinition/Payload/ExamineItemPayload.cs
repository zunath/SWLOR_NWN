using SWLOR.Game.Server.Domain.UI;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class ExamineItemPayload: GuiPayloadBase
    {
        private IItemService Item { get; }

        public string ItemName { get; set; }
        public string Description { get; set; }
        public string ItemProperties { get; set; }
        public int Weight { get; set; }
        public uint ItemObject { get; set; } = OBJECT_INVALID;
        public string ItemId { get; set; }
        public BaseItem ItemType { get; set; } = BaseItem.Invalid;
        public int ItemDMG { get; set; }
        public bool HasItemDMG { get; set; }
        public bool IsIdentified { get; set; } = true;
        public string BaseItemName { get; set; } = string.Empty;
        public string IconResref { get; set; } = string.Empty;
        public int IconSlotWidth { get; set; } = 1;
        public int IconSlotHeight { get; set; } = 1;
        public int StackSize { get; set; } = 1;
        public int Charges { get; set; }

        public ExamineItemPayload(IItemService item, uint itemObject, bool trackLiveItem = false)
        {
            Item = item;
            var type = GetBaseItemType(itemObject);
            ItemType = type;
            IsIdentified = GetIdentified(itemObject);
            BaseItemName = GetBaseItemName(type);
            ItemName = IsIdentified ? GetName(itemObject) : ExamineItemSheet.UnidentifiedTitle(BaseItemName);
            Description = GetDescription(itemObject, false, IsIdentified);
            IconResref = Item.GetIconResref(itemObject);
            IconSlotWidth = GetBaseItemSlots(type, "InvSlotWidth");
            IconSlotHeight = GetBaseItemSlots(type, "InvSlotHeight");
            if (trackLiveItem)
            {
                ItemObject = itemObject;
                ItemId = GetObjectUUID(itemObject);
            }
            Capture(itemObject);
        }

        public ExamineItemPayload(IItemService item, string itemName, string description, string itemProperties)
        {
            Item = item;
            ItemName = itemName;
            Description = description;
            ItemProperties = ExamineItemSheet.SortProperties(itemProperties);
        }

        /// <summary>Reads the values that change while the item stays in the player's possession.</summary>
        public void Capture(uint item)
        {
            ItemProperties = IsIdentified
                ? ExamineItemSheet.SortProperties(Item.BuildItemPropertyString(item))
                : string.Empty;
            Weight = GetWeight(item);
            StackSize = GetItemStackSize(item);
            Charges = GetItemCharges(item);
            ItemDMG = Item.GetDMG(item);
            HasItemDMG = GetItemHasItemProperty(item, ItemPropertyType.DMG);
        }

        private static string GetBaseItemName(BaseItem type)
        {
            var strRef = StringToInt(Get2DAString("baseitems", "Name", (int)type));
            return strRef > 0 ? GetStringByStrRef(strRef) : string.Empty;
        }

        private static int GetBaseItemSlots(BaseItem type, string column) =>
            int.TryParse(Get2DAString("baseitems", column, (int)type), out var slots) && slots > 0 ? slots : 1;
    }
}
