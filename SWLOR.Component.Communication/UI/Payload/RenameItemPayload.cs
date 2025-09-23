using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Communication.UI.Payload
{
    public class RenameItemPayload: IGuiPayload
    {
        public uint Target { get; set; }
        public RenameItemPayload(uint target)
        {
            Target = target;
        }
    }
}
