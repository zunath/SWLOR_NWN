using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class ExamineItemPayload: GuiPayloadBase
    {
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

        public ExamineItemPayload(uint item, bool trackLiveItem = false)
        {
            var type = GetBaseItemType(item);
            ItemType = type;
            IsIdentified = GetIdentified(item);
            BaseItemName = GetBaseItemName(type);
            ItemName = IsIdentified ? GetName(item) : ExamineItemSheet.UnidentifiedTitle(BaseItemName);
            Description = GetDescription(item, false, IsIdentified);
            IconResref = Service.Item.GetIconResref(item);
            IconSlotWidth = GetBaseItemSlots(type, "InvSlotWidth");
            IconSlotHeight = GetBaseItemSlots(type, "InvSlotHeight");
            if (trackLiveItem)
            {
                ItemObject = item;
                ItemId = GetObjectUUID(item);
            }
            Capture(item);
        }

        public ExamineItemPayload(string itemName, string description, string itemProperties)
        {
            ItemName = itemName;
            Description = description;
            ItemProperties = ExamineItemSheet.SortProperties(itemProperties);
        }

        /// <summary>Reads the values that change while the item stays in the player's possession.</summary>
        public void Capture(uint item)
        {
            ItemProperties = IsIdentified
                ? ExamineItemSheet.SortProperties(Service.Item.BuildItemPropertyString(item))
                : string.Empty;
            Weight = GetWeight(item);
            StackSize = GetItemStackSize(item);
            Charges = GetItemCharges(item);
            ItemDMG = Service.Item.GetDMG(item);
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
