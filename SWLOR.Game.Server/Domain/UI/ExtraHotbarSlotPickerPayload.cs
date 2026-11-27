namespace SWLOR.Game.Server.Domain.UI
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
