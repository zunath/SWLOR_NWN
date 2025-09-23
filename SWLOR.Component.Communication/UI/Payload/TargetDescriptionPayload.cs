using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Communication.UI.Payload
{
    public class TargetDescriptionPayload : IGuiPayload
    {
        public uint Target { get; set; }
        public TargetDescriptionPayload(uint target)
        {
            Target = target;
        }
    }
}
