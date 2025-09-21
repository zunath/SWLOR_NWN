using SWLOR.Shared.UI.Model;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
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
