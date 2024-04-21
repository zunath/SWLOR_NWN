using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
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
