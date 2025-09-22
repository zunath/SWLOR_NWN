using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Communication.UI.Payload
{
    public class TargetDescriptionPayload : GuiPayloadBase
    {
        public uint Target { get; set; }
        public TargetDescriptionPayload(uint target)
        {
            Target = target;
        }
    }
}
