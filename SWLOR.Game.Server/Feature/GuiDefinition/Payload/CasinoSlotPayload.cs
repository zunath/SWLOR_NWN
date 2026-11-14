using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
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
