using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class ExamineItemPayload: GuiPayloadBase
    {
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string ItemProperties { get; set; }
        public uint ItemObject { get; set; } = OBJECT_INVALID;
        public string ItemId { get; set; }
        public BaseItem ItemType { get; set; } = BaseItem.Invalid;
        public int ItemDMG { get; set; }
        public bool HasItemDMG { get; set; }
        public bool IsIdentified { get; set; } = true;

        public ExamineItemPayload(uint item, bool trackLiveItem = false)
            : this(GetName(item),
                GetDescription(item, false, GetIdentified(item)),
                GetIdentified(item) ? Service.Item.BuildItemPropertyString(item) : string.Empty)
        {
            IsIdentified = GetIdentified(item);
            if (trackLiveItem)
            {
                ItemObject = item;
                ItemId = GetObjectUUID(item);
            }
            ItemType = GetBaseItemType(item);
            ItemDMG = Service.Item.GetDMG(item);
            HasItemDMG = GetItemHasItemProperty(item, ItemPropertyType.DMG);
        }

        public ExamineItemPayload(string itemName, string description, string itemProperties)
        {
            ItemName = itemName;
            Description = description;
            ItemProperties = itemProperties;
        }
    }
}
