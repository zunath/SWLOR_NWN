using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Inventory.UI.Payload
{
    public class RenameItemPayload: GuiPayloadBase
    {
        public uint Target { get; set; }
        public RenameItemPayload(uint target)
        {
            Target = target;
        }
    }
}
