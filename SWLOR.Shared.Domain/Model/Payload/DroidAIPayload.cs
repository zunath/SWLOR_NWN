using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Associate.UI.Payload
{
    public class DroidAIPayload: IGuiPayload
    {
        public uint ControllerItem { get; set; }

        public DroidAIPayload(uint controllerItem)
        {
            ControllerItem = controllerItem;
        }
    }
}
