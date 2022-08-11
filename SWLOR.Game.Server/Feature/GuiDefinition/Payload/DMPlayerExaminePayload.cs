using SWLOR.Game.Server.Service.GuiService;

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
