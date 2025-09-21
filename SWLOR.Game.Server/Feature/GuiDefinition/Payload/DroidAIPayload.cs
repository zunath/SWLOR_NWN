using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.UI.Model;

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
