using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class DroidProgrammingPayload: GuiPayloadBase
    {
        public uint Controller { get; set; }

        public DroidProgrammingPayload(uint controller)
        {
            Controller = controller;
        }
    }
}
