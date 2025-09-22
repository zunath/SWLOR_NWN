using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Associate.UI.Payload
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
