using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Admin.UI.Payload
{
    public class DMPlayerExaminePayload: IGuiPayload
    {
        public uint Target { get; set; }

        public DMPlayerExaminePayload(uint target)
        {
            Target = target;
        }
    }
}
