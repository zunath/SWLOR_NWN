namespace SWLOR.Game.Server.Domain.UI
{
    public class CasinoSlotPayload : GuiPayloadBase
    {
        public uint Machine { get; }

        public CasinoSlotPayload(uint machine)
        {
            Machine = machine;
        }
    }
}
