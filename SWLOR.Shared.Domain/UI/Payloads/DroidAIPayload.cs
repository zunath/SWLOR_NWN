using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Domain.UI.Payloads
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
