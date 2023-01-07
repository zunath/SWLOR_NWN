using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class DroidAIPayload: GuiPayloadBase
    {
        public uint ControllerItem { get; set; }

        public DroidAIPayload(uint controllerItem)
        {
            ControllerItem = controllerItem;
        }
    }
}
