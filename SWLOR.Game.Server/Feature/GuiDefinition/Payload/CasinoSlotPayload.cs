using SWLOR.Game.Server.Service.GuiService;

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
