using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
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
