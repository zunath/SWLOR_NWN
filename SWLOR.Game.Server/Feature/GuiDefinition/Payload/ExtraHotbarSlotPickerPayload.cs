using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class ExtraHotbarSlotPickerPayload : GuiPayloadBase
    {
        public int BarIndex { get; }
        public int SlotIndex { get; }

        public ExtraHotbarSlotPickerPayload(int barIndex, int slotIndex)
        {
            BarIndex = barIndex;
            SlotIndex = slotIndex;
        }
    }
}
