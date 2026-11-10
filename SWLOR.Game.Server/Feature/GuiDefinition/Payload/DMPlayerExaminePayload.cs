using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class DMPlayerExaminePayload: GuiPayloadBase
    {
        public uint Target { get; set; }

        public DMPlayerExaminePayload(uint target)
        {
            Target = target;
        }
    }
}
